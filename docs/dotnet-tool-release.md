# Public `sqloom` `.NET tool` Release

This runbook is for publishing a new public version of the `sqloom` dotnet tool to NuGet.org when package upload is done manually after the local build completes.

Run every command from the repo root.

## What gets published

The public tool install depends on one version-aligned package set:

- `Sqloom.Core`
- `Sqloom.QueryStore`
- `Sqloom.AzureSql`
- `Sqloom.AspNetCore`
- `sqloom`

The release version comes from `Directory.Build.props`. The tool package metadata lives in `src/Sqloom.Host/Sqloom.Host.csproj`, and the package readme comes from `src/Sqloom.Host/PackageReadme.md`.

## 1. Update release metadata

1. Set the new `<Version>` in `Directory.Build.props` using a bare NuGet version such as `0.1.0`. Use the leading `v` only for Git tags or release titles such as `v0.1.0`.
2. Confirm `src/Sqloom.Host/Sqloom.Host.csproj` still has the correct public package metadata: `PackageId` is `sqloom`, `ToolCommandName` is `sqloom`, and `PackageProjectUrl`, `RepositoryUrl`, `PackageLicenseExpression`, and `PackageTags` are correct.
3. Confirm `src/Sqloom.Host/PackageReadme.md` still matches the current CLI behavior and install story.
4. If the public CLI surface or documented workflow changed, update `README.md` in the same change.

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
4. Packs the five publishable projects into that folder feed.
5. Verifies that every expected `.nupkg` exists.
6. Installs `sqloom` from that local feed into `.\artifacts\tools\sqloom-verify`.
7. Runs `sqloom.exe --help`.
8. Runs a sample `replay` smoke test unless `-SkipSmoke` is passed.
9. Prints the exact `dotnet nuget push` commands for the produced packages.

Use `-SkipSmoke` only when the sample replay cannot run in the current environment and you are intentionally accepting a weaker release gate:

```powershell
pwsh .\scripts\prepare-sqloom-packages.ps1 -SkipSmoke
```

## 4. Inspect the release output

After the script succeeds, confirm these packages exist under `.\artifacts\packages\sqloom` with the same version number:

- `Sqloom.Core.<version>.nupkg`
- `Sqloom.QueryStore.<version>.nupkg`
- `Sqloom.AzureSql.<version>.nupkg`
- `Sqloom.AspNetCore.<version>.nupkg`
- `sqloom.<version>.nupkg`

The verification install should also exist under `.\artifacts\tools\sqloom-verify`.

If you want one more explicit local check before upload, run:

```powershell
.\artifacts\tools\sqloom-verify\sqloom.exe --version
.\artifacts\tools\sqloom-verify\sqloom.exe --help
```

## 5. Upload the packages manually to NuGet.org

The packaging script already prints `dotnet nuget push` commands. For a manual browser upload flow, use those printed paths as a package manifest and upload the `.nupkg` files yourself instead of running the push commands.

Upload in this order:

1. `Sqloom.Core.<version>.nupkg`
2. `Sqloom.QueryStore.<version>.nupkg`
3. `Sqloom.AzureSql.<version>.nupkg`
4. `Sqloom.AspNetCore.<version>.nupkg`
5. `sqloom.<version>.nupkg`

Upload the tool package last because it depends on the other four packages being available from the public feed.

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

If this fails because a dependency package is not visible yet, wait for NuGet indexing to finish and try again.

## 7. If something is wrong after upload

Do not try to reuse the same version number. Fix the repo, bump the version, rerun this runbook, and publish a new package set.
