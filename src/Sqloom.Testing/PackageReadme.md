# Sqloom.Testing

`Sqloom.Testing` contains the public harness API and shared pipeline surface for running Sqloom against an ASP.NET Core application. App-owned harness projects implement `ISqloomApplication`, describe their OpenAPI source and replay profile, and start an `ISqloomApplicationSession` that Sqloom can replay through.

Install the package in the harness project:

```powershell
dotnet add package Sqloom.Testing
```

The package also contains the shared `Sqloom.Pipeline.*` namespaces for replay, Query Store, artifact, and advice pipeline types, so harness projects only need this one library package.

Install the Sqloom CLI separately:

```powershell
dotnet tool install --global sqloom
```

The CLI accepts a harness project, harness assembly, solution, solution filter, or directory containing harness projects. A resolved target must contain exactly one public non-abstract `ISqloomApplication` implementation.
