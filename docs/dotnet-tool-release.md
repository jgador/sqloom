# Public Sqloom Package Release

This runbook is for publishing a new public version of the `sqloom` dotnet tool and the harness contract package set to NuGet.org when package upload is done manually after the local build completes.

Run every command from the repo root.

## What gets published

The public release uploads these NuGet packages:

- `sqloom`
- `Sqloom.Core`
- `Sqloom.Testing`

The release version comes from `Directory.Build.props`. The tool package metadata lives in `src/Sqloom.Host/Sqloom.Host.csproj`, and the package readme comes from `src/Sqloom.Host/PackageReadme.md`. The harness contract package metadata lives in `src/Sqloom.Testing/Sqloom.Testing.csproj`. `Sqloom.Core` is published because `Sqloom.Testing` has public API dependencies on shared runner and Query Store profile types.

## 1. Update release metadata

1. Set the new `<Version>` in `Directory.Build.props` using a bare NuGet version such as `0.1.0`. Use the leading `v` only for Git tags or release titles such as `v0.1.0`.
2. Confirm `src/Sqloom.Host/Sqloom.Host.csproj` still has the correct public package metadata: `PackageId` is `sqloom`, `ToolCommandName` is `sqloom`, and `PackageProjectUrl`, `RepositoryUrl`, `PackageLicenseExpression`, and `PackageTags` are correct.
3. Confirm `src/Sqloom.Core/Sqloom.Core.csproj` and `src/Sqloom.Testing/Sqloom.Testing.csproj` still have correct public package metadata for the harness contract dependency graph.
4. Confirm `src/Sqloom.Host/PackageReadme.md` still matches the current CLI behavior and install story.
5. If the public CLI surface, harness contract surface, or documented workflow changed, update `README.md` in the same change.

## 2. Validate the repo before packing

Run the standard repo validation lane first:

```powershell
dotnet restore .\Sqloom.slnx
dotnet build .\Sqloom.slnx --tl:off --nologo "-clp:ErrorsOnly;NoSummary"
dotnet test --solution .\Sqloom.UnitTests.slnf
dotnet test --solution .\Sqloom.IntegrationTests.slnf
```

If the release includes CLI behavior changes and you want an extra local-tool sanity check before packing, also run:

```powershell
pwsh .\scripts\deploy-sqloom-local.ps1
```

## 3. Build the release packages

Run the package-prep script:

```powershell
pwsh .\scripts\prepare-sqloom-packages.ps1
```

That script is the main release gate for packaging. It:

1. Restores `.\Sqloom.slnx`.
2. Builds `.\Sqloom.slnx` in `Release`.
3. Recreates the local package feed at `.\artifacts\packages\sqloom`.
4. Packs the tool project, `Sqloom.Core`, and `Sqloom.Testing` into that folder feed for local verification.
5. Verifies that every expected `.nupkg` exists for the local pack step.
6. Installs `sqloom` from that local feed into `.\artifacts\tools\sqloom-verify`.
7. Runs `sqloom.exe --help`.
8. Runs a sample `replay` smoke test unless `-SkipSmoke` is passed.
9. Prints the exact `dotnet nuget push` commands for the public package set.

Use `-SkipSmoke` only when the sample replay cannot run in the current environment and you are intentionally accepting a weaker release gate:

```powershell
pwsh .\scripts\prepare-sqloom-packages.ps1 -SkipSmoke
```

## 4. Inspect the release output

After the script succeeds, confirm the public packages exist under `.\artifacts\packages\sqloom`:

- `sqloom.<version>.nupkg`
- `Sqloom.Core.<version>.nupkg`
- `Sqloom.Testing.<version>.nupkg`

The verification install should also exist under `.\artifacts\tools\sqloom-verify`.

If you want one more explicit local check before upload, run:

```powershell
.\artifacts\tools\sqloom-verify\sqloom.exe --version
.\artifacts\tools\sqloom-verify\sqloom.exe --help
```

## 5. Upload the packages manually to NuGet.org

The packaging script already prints the `dotnet nuget push` commands for the public packages. For a manual browser upload flow, use those printed paths as the package manifests and upload the `.nupkg` files yourself instead of running the push commands.

Upload:

1. `Sqloom.Core.<version>.nupkg`
2. `Sqloom.Testing.<version>.nupkg`
3. `sqloom.<version>.nupkg`

## 6. Verify install from the public feed

After NuGet.org finishes validating and indexing the upload, verify that a fresh install works from the public feed:

```powershell
$version = "<version>"
$toolPath = Join-Path $PWD "artifacts\tools\sqloom-public"

if (Test-Path -LiteralPath $toolPath)
{
    Remove-Item -LiteralPath $toolPath -Recurse -Force
}

dotnet tool install --tool-path $toolPath sqloom --version $version
& (Join-Path $toolPath "sqloom.exe") --version
& (Join-Path $toolPath "sqloom.exe") --help
```

If this fails immediately after upload, wait for NuGet indexing to finish and try again.

## 7. If something is wrong after upload

Do not try to reuse the same version number. Fix the repo, bump the version, rerun this runbook, and publish a new package set.
