#!/usr/bin/env dotnet
#:project ../src/Sqloom.Host/Sqloom.Host.csproj

using System;
using System.IO;
using System.Text;
using Sqloom.Host;

return Run(args);

static int Run(string[] args)
{
    if (args.Length != 1 || args[0] is not ("--write" or "--check"))
    {
        Console.Error.WriteLine("Usage: dotnet run --file .\\tools\\Sqloom.CommandDocs.cs -- --write|--check");
        return 2;
    }

    var repositoryRoot = FindRepositoryRoot();
    var outputPath = Path.Combine(
        repositoryRoot,
        CommandReferenceMarkdown.RelativePath.Replace('/', Path.DirectorySeparatorChar));
    var expected = CommandReferenceMarkdown.Render();

    if (args[0] == "--write")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, expected, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"Wrote {Path.GetRelativePath(repositoryRoot, outputPath)}");
        return 0;
    }

    if (!File.Exists(outputPath))
    {
        Console.Error.WriteLine($"Missing generated command reference: {Path.GetRelativePath(repositoryRoot, outputPath)}");
        Console.Error.WriteLine("Run: dotnet run --file .\\tools\\Sqloom.CommandDocs.cs -- --write");
        return 1;
    }

    var actual = NormalizeNewlines(File.ReadAllText(outputPath));
    if (!string.Equals(actual, NormalizeNewlines(expected), StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"Generated command reference is stale: {Path.GetRelativePath(repositoryRoot, outputPath)}");
        Console.Error.WriteLine("Run: dotnet run --file .\\tools\\Sqloom.CommandDocs.cs -- --write");
        return 1;
    }

    Console.WriteLine("Sqloom command reference is up to date.");
    return 0;
}

static string FindRepositoryRoot()
{
    var entryPointPath = AppContext.GetData("EntryPointFilePath") as string
        ?? throw new InvalidOperationException("The file-based app entry point path is unavailable.");
    var directory = new DirectoryInfo(Path.GetDirectoryName(entryPointPath)!);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Sqloom.slnx")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new InvalidOperationException("Could not locate the Sqloom repository root.");
}

static string NormalizeNewlines(string value)
{
    return value.Replace("\r\n", "\n", StringComparison.Ordinal);
}
