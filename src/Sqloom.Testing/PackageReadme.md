# Sqloom.Testing

`Sqloom.Testing` contains the public harness API and shared pipeline surface for running Sqloom against an ASP.NET Core application. App-owned file-based or project-backed harnesses implement `ISqloomApplication`, describe their replay profile, and start an `ISqloomApplicationSession` that Sqloom can replay through.

Reference the package from a .NET 10 C# file-based harness:

```csharp
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property ManagePackageVersionsCentrally=false
#:package Sqloom.Testing@<sqloom-version>
#:project <relative-app-project.csproj>
```

Existing project-backed harnesses use a normal package reference:

```powershell
dotnet add package Sqloom.Testing
```

The package also contains the shared `Sqloom.Pipeline.*` namespaces for replay, Query Store, artifact, and advice pipeline types, so harnesses only need this one library package.

## Replay data generation contract

The public replay-data API and persisted artifacts use **generation** for the agent workflow that fills missing HTTP path, query, header, and body inputs. Consumers upgrading from the earlier preparation-named contract must update these references:

| Earlier name | Current name |
| --- | --- |
| `ReplayDataPreparationReport` | `ReplayDataGenerationReport` |
| `ReplayDataPreparationOperation` | `ReplayDataGenerationOperation` |
| `ArtifactLayout.GetReplayDataPreparationPath(...)` | `ArtifactLayout.GetReplayDataGenerationPath(...)` |
| `EndpointReplayRunResult.ReplayDataPreparationPath` | `EndpointReplayRunResult.ReplayDataGenerationPath` |
| `EndpointReplayRunResult.ReplayDataPreparation` | `EndpointReplayRunResult.ReplayDataGeneration` |
| `replay-data-prep.json` | `replay-data-generation.json` |
| `replayDataPreparationPath` | `replayDataGenerationPath` |
| `replayDataPreparation` | `replayDataGeneration` |

This is a breaking API and JSON contract rename: Sqloom does not emit aliases or a duplicate legacy artifact. `ReplayPreparedData` and its `preparedData` JSON property retain their names because they represent the values after generation, ready to apply to a replay request.

Install the Sqloom CLI separately:

```powershell
dotnet tool install --global sqloom
```

The CLI accepts an explicit .NET 10 C# file-based harness, harness project, harness assembly, solution, solution filter, or directory containing harness projects. Sqloom always builds `.cs` targets and rejects `--no-build` for them. Every resolved target must contain exactly one public non-abstract `ISqloomApplication` implementation.
