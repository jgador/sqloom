using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Sqloom.Testing;

namespace Sqloom.Host;

/// <summary>
/// Loads Sqloom application harnesses from the resolved target projects or assemblies.
/// </summary>
internal static class AppResolver
{
    private static readonly object _defaultProbeDirectoryLock = new();
    private static readonly List<AssemblyProbe> _defaultAssemblyProbes = [];
    private static bool _defaultAssemblyResolverRegistered;

    public static async Task<ISqloomApplication> ResolveAsync(
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        var targetPath = GetRequiredTargetPath(startupOptions);
        var assemblySelections = await AppProjectResolver
            .ResolveAssemblySelectionsAsync(
                targetPath,
                startupOptions.NoBuild,
                startupOptions.DotNetCommand,
                cancellationToken)
            .ConfigureAwait(false);
        return CreateApplication(
            targetPath,
            assemblySelections);
    }

    public static Task<string> ResolveAssemblyPathAsync(
        HostStartupOptions startupOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        return AppProjectResolver.ResolveAssemblyPathAsync(
            GetRequiredTargetPath(startupOptions),
            startupOptions.NoBuild,
            startupOptions.DotNetCommand,
            cancellationToken);
    }

    private static ISqloomApplication CreateApplication(
        string targetPath,
        IReadOnlyList<ResolvedAssemblySelection> assemblySelections)
    {
        // Gather public concrete ISqloomApplication implementations across all resolved assemblies,
        // then require the target to identify exactly one harness application.
        List<(Type Type, string AssemblyPath)> appTypes = [];
        foreach (var assemblySelection in assemblySelections)
        {
            var assembly = LoadAppAssembly(assemblySelection);
            appTypes.AddRange(GetSqloomApplicationTypes(
                assembly,
                assemblySelection.AssemblyPath));
        }

        var selectedType = appTypes.Count switch
        {
            0 => throw new AppResolutionException(
                $"The Sqloom target '{Path.GetFullPath(targetPath)}' does not contain an {nameof(ISqloomApplication)} implementation."),
            > 1 => throw new AppResolutionException(
                $"The Sqloom target '{Path.GetFullPath(targetPath)}' contains multiple public {nameof(ISqloomApplication)} implementations: {string.Join(", ", appTypes.Select(static item => item.Type.FullName))}. Pass a narrower target."),
            _ => appTypes[0],
        };

        return CreateApplication(selectedType);
    }

    private static ISqloomApplication CreateApplication((Type Type, string AssemblyPath) appType)
    {
        try
        {
            if (Activator.CreateInstance(appType.Type) is not ISqloomApplication application)
            {
                throw new AppResolutionException(
                    $"Could not create an {nameof(ISqloomApplication)} from '{appType.Type.FullName}' in '{appType.AssemblyPath}'.");
            }

            return application;
        }
        catch (AppResolutionException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is BadImageFormatException
                or FileLoadException
                or FileNotFoundException
                or MissingMethodException
                or TargetInvocationException)
        {
            throw new AppResolutionException(
                $"Failed to load the Sqloom harness assembly '{appType.AssemblyPath}': {exception.Message}",
                exception);
        }
    }

    private static string GetRequiredTargetPath(HostStartupOptions startupOptions)
    {
        if (!string.IsNullOrWhiteSpace(startupOptions.AppTargetPath))
        {
            return Path.GetFullPath(startupOptions.AppTargetPath);
        }

        throw new AppResolutionException(
            "Sqloom now requires an explicit harness target path after the stage verb. Use tune <path>, replay <path>, or observe <path> so the standalone host can resolve an app harness.");
    }

    private static Assembly LoadAppAssembly(ResolvedAssemblySelection assemblySelection)
    {
        var fullAssemblyPath = Path.GetFullPath(assemblySelection.AssemblyPath);
        if (assemblySelection.TargetSelection.Kind == ResolvedTargetKind.CSharpFile)
        {
            FileHarnessLoadContext loadContext = new(fullAssemblyPath);
            return loadContext.LoadFromAssemblyPath(fullAssemblyPath);
        }

        return LoadAssemblyWithRegisteredProbe(
            AssemblyLoadContext.Default,
            fullAssemblyPath);
    }

    private static void RegisterDefaultAssemblyProbe(string assemblyPath)
    {
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath)
            ?? throw new InvalidOperationException($"The Sqloom harness assembly path '{assemblyPath}' has no parent directory.");
        AssemblyProbe assemblyProbe = new(
            assemblyPath,
            assemblyDirectory,
            new AssemblyDependencyResolver(assemblyPath));

        lock (_defaultProbeDirectoryLock)
        {
            if (!_defaultAssemblyResolverRegistered)
            {
                AssemblyLoadContext.Default.Resolving += ResolveFromRegisteredProbeDirectories;
                _defaultAssemblyResolverRegistered = true;
            }

            if (_defaultAssemblyProbes.Any(existingProbe =>
                    string.Equals(
                        existingProbe.MainAssemblyPath,
                        assemblyProbe.MainAssemblyPath,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            _defaultAssemblyProbes.Add(assemblyProbe);
        }
    }

    private static Assembly? ResolveFromRegisteredProbeDirectories(
        AssemblyLoadContext loadContext,
        AssemblyName assemblyName)
    {
        var existingAssembly = FindLoadedDefaultAssembly(assemblyName.Name ?? string.Empty);
        if (existingAssembly is not null)
        {
            return existingAssembly;
        }

        AssemblyProbe[] assemblyProbes;
        lock (_defaultProbeDirectoryLock)
        {
            assemblyProbes = _defaultAssemblyProbes.ToArray();
        }

        foreach (var assemblyProbe in assemblyProbes)
        {
            var resolvedAssemblyPath = assemblyProbe.DependencyResolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrWhiteSpace(resolvedAssemblyPath)
                && File.Exists(resolvedAssemblyPath))
            {
                return LoadAssemblyWithRegisteredProbe(
                    loadContext,
                    resolvedAssemblyPath);
            }

            var candidatePath = Path.Combine(assemblyProbe.AssemblyDirectory, $"{assemblyName.Name}.dll");
            if (File.Exists(candidatePath))
            {
                return LoadAssemblyWithRegisteredProbe(
                    loadContext,
                    candidatePath);
            }
        }

        return null;
    }

    private static Assembly? FindLoadedDefaultAssembly(string assemblyName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly =>
                AssemblyLoadContext.GetLoadContext(assembly) == AssemblyLoadContext.Default
                && string.Equals(
                    assembly.GetName().Name,
                    assemblyName,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<(Type Type, string AssemblyPath)> GetSqloomApplicationTypes(
        Assembly assembly,
        string assemblyPath)
    {
        return GetLoadableTypes(assembly, assemblyPath)
            .Where(static type =>
                !type.IsAbstract
                && !type.IsInterface
                && (type.IsPublic || type.IsNestedPublic)
                && typeof(ISqloomApplication).IsAssignableFrom(type))
            .Select(type => (
                Type: type,
                AssemblyPath: assemblyPath))
            .ToArray();
    }

    private static Type[] GetLoadableTypes(Assembly assembly, string assemblyPath)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var loaderMessage = exception.LoaderExceptions
                .FirstOrDefault(static item => item is not null)?
                .Message
                ?? "Unknown loader error.";
            throw new AppResolutionException(
                $"Failed to inspect the Sqloom harness assembly '{assemblyPath}': {loaderMessage}",
                exception);
        }
    }

    private static Assembly LoadAssemblyWithRegisteredProbe(
        AssemblyLoadContext loadContext,
        string assemblyPath)
    {
        var fullAssemblyPath = Path.GetFullPath(assemblyPath);

        // Register the dependency probe before loading through the default context so harness
        // dependencies resolve from the original target directory.
        RegisterDefaultAssemblyProbe(fullAssemblyPath);

        var assemblyName = AssemblyName.GetAssemblyName(fullAssemblyPath).Name
            ?? throw new InvalidOperationException($"Could not determine the assembly name for '{assemblyPath}'.");
        var existingAssembly = FindLoadedDefaultAssembly(assemblyName);
        return existingAssembly
            ?? loadContext.LoadFromAssemblyPath(fullAssemblyPath);
    }

    /// <summary>
    /// Keeps a loaded harness assembly paired with the dependency resolver for its original directory.
    /// </summary>
    private sealed record AssemblyProbe(
        string MainAssemblyPath,
        string AssemblyDirectory,
        AssemblyDependencyResolver DependencyResolver);

    private sealed class FileHarnessLoadContext : AssemblyLoadContext
    {
        private static readonly Assembly[] SharedAssemblies =
        [
            typeof(ISqloomApplication).Assembly,
            typeof(Microsoft.EntityFrameworkCore.DeleteBehavior).Assembly,
            typeof(Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor).Assembly,
            typeof(Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor).Assembly,
        ];
        private readonly AssemblyDependencyResolver _dependencyResolver;

        internal FileHarnessLoadContext(string mainAssemblyPath)
            : base(
                $"Sqloom.FileHarness.{Guid.NewGuid():N}",
                isCollectible: false)
        {
            _dependencyResolver = new AssemblyDependencyResolver(mainAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            foreach (var sharedAssembly in SharedAssemblies)
            {
                var sharedAssemblyName = sharedAssembly.GetName();
                if (!string.Equals(
                        assemblyName.Name,
                        sharedAssemblyName.Name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(
                        assemblyName.FullName,
                        sharedAssemblyName.FullName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new FileLoadException(
                        $"File harness dependency '{assemblyName.FullName}' is incompatible with " +
                        $"Sqloom's shared dependency '{sharedAssemblyName.FullName}'.");
                }

                return sharedAssembly;
            }

            var assemblyPath = _dependencyResolver.ResolveAssemblyToPath(assemblyName);
            return string.IsNullOrWhiteSpace(assemblyPath)
                ? null
                : LoadFromAssemblyPath(assemblyPath);
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            var unmanagedDllPath = _dependencyResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return string.IsNullOrWhiteSpace(unmanagedDllPath)
                ? nint.Zero
                : LoadUnmanagedDllFromPath(unmanagedDllPath);
        }
    }
}

/// <summary>
/// Represents an error while resolving a Sqloom app harness.
/// </summary>
internal sealed class AppResolutionException : Exception
{
    public AppResolutionException(string message)
        : base(message)
    {
    }

    public AppResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
