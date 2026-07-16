# Sqloom.Testing

`Sqloom.Testing` contains the public harness API and shared pipeline surface for running Sqloom against an ASP.NET Core application. App-owned file-based or project-backed harnesses implement `ISqloomApplication`, describe their OpenAPI source and replay profile, and start an `ISqloomApplicationSession` that Sqloom can replay through.

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

Install the Sqloom CLI separately:

```powershell
dotnet tool install --global sqloom
```

The CLI accepts an explicit .NET 10 C# file-based harness, harness project, harness assembly, solution, solution filter, or directory containing harness projects. Sqloom always builds `.cs` targets and rejects `--no-build` for them. Every resolved target must contain exactly one public non-abstract `ISqloomApplication` implementation.
