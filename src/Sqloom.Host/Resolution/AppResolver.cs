using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Sqloom.Core.Contracts;

namespace Sqloom.Host;

/// <summary>
/// Loads Sqloom app integrations from the resolved target projects.
/// </summary>
internal sealed class AppResolver
{
    private static readonly object _defaultProbeDirectoryLock = new();
    private static readonly List<AssemblyProbe> _defaultAssemblyProbes = [];
    private static bool _defaultAssemblyResolverRegistered;
    private readonly AppProjectResolver _projectResolver = new();

    public IAppIntegration Resolve(
        HostStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        var assemblyPath = ResolveAssemblyPath(startupOptions);
        return CreateAppIntegration(assemblyPath);
    }

    public IReadOnlyList<IAppIntegration> ResolveReplayIntegrations(HostStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        return _projectResolver
            .ResolveAssemblySelections(
                GetRequiredTargetPath(startupOptions),
                startupOptions.NoBuild,
                startupOptions.DotNetCommand)
            .Select(static selection => CreateAppIntegration(selection.AssemblyPath))
            .ToArray();
    }

    public string ResolveAssemblyPath(HostStartupOptions startupOptions)
    {
        ArgumentNullException.ThrowIfNull(startupOptions);

        return _projectResolver.ResolveAssemblyPath(
            GetRequiredTargetPath(startupOptions),
            startupOptions.NoBuild,
            startupOptions.DotNetCommand);
    }

    private static IAppIntegration CreateAppIntegration(string assemblyPath)
    {
        try
        {
            var assembly = LoadAppAssembly(assemblyPath);
            var appType = SelectAppIntegrationType(assembly, assemblyPath);
            if (Activator.CreateInstance(appType) is not IAppIntegration appIntegration)
            {
                throw new AppResolutionException(
                    $"Could not create an {nameof(IAppIntegration)} from '{appType.FullName}' in '{assemblyPath}'.");
            }

            return appIntegration;
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
                $"Failed to load the Sqloom app integration assembly '{assemblyPath}': {exception.Message}",
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
            "Sqloom now requires an explicit target path after the stage verb. Use tune <path>, replay <path>, or observe <path> so the standalone host can resolve an app integration.");
    }

    private static Assembly LoadAppAssembly(string assemblyPath)
    {
        var fullAssemblyPath = Path.GetFullPath(assemblyPath);
        return LoadAssemblyWithRegisteredProbe(
            AssemblyLoadContext.Default,
            fullAssemblyPath);
    }

    private static void RegisterDefaultAssemblyProbe(string assemblyPath)
    {
        var assemblyDirectory = Path.GetDirectoryName(assemblyPath)
            ?? throw new InvalidOperationException($"The app integration assembly path '{assemblyPath}' has no parent directory.");
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

    private static Type SelectAppIntegrationType(Assembly assembly, string assemblyPath)
    {
        var appTypes = GetLoadableTypes(assembly, assemblyPath)
            .Where(static type =>
                !type.IsAbstract
                && !type.IsInterface
                && (type.IsPublic || type.IsNestedPublic)
                && typeof(IAppIntegration).IsAssignableFrom(type))
            .ToArray();

        return appTypes.Length switch
        {
            0 => throw new AppResolutionException(
                $"The app integration assembly '{assemblyPath}' does not contain a public {nameof(IAppIntegration)} implementation."),
            > 1 => throw new AppResolutionException(
                $"The app integration assembly '{assemblyPath}' contains multiple public {nameof(IAppIntegration)} implementations: {string.Join(", ", appTypes.Select(static type => type.FullName))}."),
            _ => appTypes[0],
        };
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
                $"Failed to inspect the app integration assembly '{assemblyPath}': {loaderMessage}",
                exception);
        }
    }

    private static Assembly LoadAssemblyWithRegisteredProbe(
        AssemblyLoadContext loadContext,
        string assemblyPath)
    {
        var fullAssemblyPath = Path.GetFullPath(assemblyPath);
        RegisterDefaultAssemblyProbe(fullAssemblyPath);

        var assemblyName = AssemblyName.GetAssemblyName(fullAssemblyPath).Name
            ?? throw new InvalidOperationException($"Could not determine the assembly name for '{assemblyPath}'.");
        var existingAssembly = FindLoadedDefaultAssembly(assemblyName);
        return existingAssembly
            ?? loadContext.LoadFromAssemblyPath(fullAssemblyPath);
    }

    private sealed record AssemblyProbe(
        string MainAssemblyPath,
        string AssemblyDirectory,
        AssemblyDependencyResolver DependencyResolver);

}

/// <summary>
/// Represents an error while resolving a Sqloom app integration.
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
