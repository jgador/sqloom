# Sqloom.Core

`Sqloom.Core` contains shared contracts used by Sqloom harnesses, replay evidence, Query Store evidence, artifact models, and tuning advice payloads.

Most application harness projects should reference `Sqloom.Testing` instead of referencing `Sqloom.Core` directly. `Sqloom.Testing` brings in `Sqloom.Core` transitively.

Reference `Sqloom.Core` directly only when a project needs the shared contract and artifact types without the harness-facing ASP.NET Core helpers.

```powershell
dotnet add package Sqloom.Core
```

Install the Sqloom CLI separately when you want to run replay, observe, correlate, advise, or tune workflows:

```powershell
dotnet tool install --global sqloom
```
