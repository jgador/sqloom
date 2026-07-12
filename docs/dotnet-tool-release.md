# Public Sqloom Package Release

This runbook is for publishing a new public Sqloom version to NuGet.org when package upload is done manually after the local build completes.

Run every command from the repo root.

## What gets published

The public release uploads these NuGet packages:

- `Sqloom.Core`
- `Sqloom.Testing`
- `sqloom`

Upload `Sqloom.Core` before `Sqloom.Testing` because `Sqloom.Testing` declares a NuGet dependency on the same-version `Sqloom.Core` package. The `sqloom` dotnet tool is self-contained for normal tool installs, but app-owned harness projects need `Sqloom.Testing` as a compile-time package.

The release version comes from [Directory.Build.props](../Directory.Build.props). Library package metadata lives in [src/Sqloom.Core/Sqloom.Core.csproj](../src/Sqloom.Core/Sqloom.Core.csproj) and [src/Sqloom.Testing/Sqloom.Testing.csproj](../src/Sqloom.Testing/Sqloom.Testing.csproj). The tool package metadata lives in [src/Sqloom.Host/Sqloom.Host.csproj](../src/Sqloom.Host/Sqloom.Host.csproj), and the tool package readme comes from [src/Sqloom.Host/PackageReadme.md](../src/Sqloom.Host/PackageReadme.md).

## 1. Update release metadata

1. Set the new `<Version>` in [Directory.Build.props](../Directory.Build.props) using a bare NuGet package version. Use the leading `v` only for Git tags or release titles.
2. Confirm [src/Sqloom.Core/Sqloom.Core.csproj](../src/Sqloom.Core/Sqloom.Core.csproj) and [src/Sqloom.Testing/Sqloom.Testing.csproj](../src/Sqloom.Testing/Sqloom.Testing.csproj) still have correct public package metadata: descriptions, project URL, repository URL, license expression, and package tags.
3. Confirm [src/Sqloom.Host/Sqloom.Host.csproj](../src/Sqloom.Host/Sqloom.Host.csproj) still has the correct public tool package metadata: `PackageId` is `sqloom`, `ToolCommandName` is `sqloom`, and `PackageProjectUrl`, `RepositoryUrl`, `PackageLicenseExpression`, and `PackageTags` are correct.
4. Confirm [src/Sqloom.Host/PackageReadme.md](../src/Sqloom.Host/PackageReadme.md) still matches the current install story and points exact command syntax back to the generated command reference instead of duplicating option tables.
5. If command metadata changed, update [src/Sqloom.Host/CommandCatalog.cs](../src/Sqloom.Host/CommandCatalog.cs), regenerate [.agents/skills/sqloom/references/commands.md](../.agents/skills/sqloom/references/commands.md), and verify it with the generator check command.
6. If the public CLI surface, harness contract surface, or documented workflow changed, update [README.md](../README.md) in the same change without duplicating generated command tables.

## 2. Validate the repo before packing

Run the standard repo validation lane first:

```powershell
dotnet run --file .\tools\Sqloom.CommandDocs.cs -- --check
dotnet restore .\Sqloom.slnx
dotnet build .\Sqloom.slnx --tl:off --nologo "-clp:ErrorsOnly;NoSummary"
dotnet test --solution .\Sqloom.UnitTests.slnf
dotnet test --solution .\Sqloom.IntegrationTests.slnf
```

If the release includes CLI behavior changes and you want an extra local-tool sanity check before packing, also run [scripts/deploy-sqloom-local.ps1](../scripts/deploy-sqloom-local.ps1):

```powershell
pwsh .\scripts\deploy-sqloom-local.ps1
```

## 3. Build the release packages

Run [scripts/prepare-sqloom-packages.ps1](../scripts/prepare-sqloom-packages.ps1):

```powershell
pwsh .\scripts\prepare-sqloom-packages.ps1
```

That script is the main release gate for packaging. It:

1. Restores `.\Sqloom.slnx`.
2. Builds `.\Sqloom.slnx` in `Release`.
3. Recreates the local package feed at `.\artifacts\packages\sqloom`.
4. Packs `Sqloom.Core`, `Sqloom.Testing`, and the tool project into that folder feed for local verification.
5. Verifies that every expected `.nupkg` exists for the local pack step.
6. Builds a temporary consumer project that references `Sqloom.Testing` from the folder feed, proving that the `Sqloom.Core` transitive dependency resolves.
7. Installs `sqloom` from that local feed into `.\artifacts\tools\sqloom-verify`.
8. Runs `sqloom.exe --help`.
9. Runs a sample `replay` smoke test unless `-SkipSmoke` is passed.
10. Prints the exact `dotnet nuget push` commands for the public packages in dependency order.

Use `-SkipSmoke` only when the sample replay cannot run in the current environment and you are intentionally accepting a weaker release gate:

```powershell
pwsh .\scripts\prepare-sqloom-packages.ps1 -SkipSmoke
```

To dogfood the package before upload, install or update the locally packed `sqloom` from the folder feed into the current user's global .NET tool location:

```powershell
$version = (Select-Xml -Path .\Directory.Build.props -XPath '/Project/PropertyGroup/Version').Node.InnerText; if (dotnet tool list --global | Select-String -Pattern '^sqloom\s') { dotnet tool update --global sqloom --version $version --source .\artifacts\packages\sqloom --allow-downgrade } else { dotnet tool install --global sqloom --version $version --source .\artifacts\packages\sqloom }; sqloom --version; Get-Command sqloom | Select-Object -ExpandProperty Source
```

On Windows this installs to `$HOME\.dotnet\tools`. This uses the freshly packed local feed, not NuGet.org, and is intended for final dogfooding before upload.

## 4. Inspect the release output

After the script succeeds, confirm the public packages exist under `.\artifacts\packages\sqloom`:

- `Sqloom.Core.<version>.nupkg`
- `Sqloom.Testing.<version>.nupkg`
- `sqloom.<version>.nupkg`

The verification install should also exist under `.\artifacts\tools\sqloom-verify`, and the temporary `Sqloom.Testing` consumer check should exist under `.\artifacts\tools\sqloom-testing-verify`.

If you want one more explicit local check before upload, run:

```powershell
.\artifacts\tools\sqloom-verify\sqloom.exe --version
.\artifacts\tools\sqloom-verify\sqloom.exe --help
```

If you installed from the local feed globally, verify the global tool shim directly:

```powershell
& (Join-Path $HOME ".dotnet\tools\sqloom.exe") --version; Get-Command sqloom | Select-Object -ExpandProperty Source
```

## 5. Upload the package manually to NuGet.org

The packaging script already prints the `dotnet nuget push` commands for the public packages. For a manual browser upload flow, use those printed paths as the package manifests and upload the `.nupkg` files yourself instead of running the push commands.

Upload in this order:

1. `Sqloom.Core.<version>.nupkg`
2. `Sqloom.Testing.<version>.nupkg`
3. `sqloom.<version>.nupkg`

## 6. Verify install from the public feed

After NuGet.org finishes validating and indexing the upload, verify that a fresh harness package restore and global tool install work from the public feed:

```powershell
$version = (Select-Xml -Path .\Directory.Build.props -XPath '/Project/PropertyGroup/Version').Node.InnerText
$verifyRoot = ".\artifacts\tools\sqloom-testing-public-verify"
Remove-Item $verifyRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $verifyRoot | Out-Null
Set-Content -Path (Join-Path $verifyRoot "SqloomTestingPublicVerify.csproj") -Value @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
    <PackageReference Include="Sqloom.Testing" Version="$version" />
  </ItemGroup>
</Project>
"@
Set-Content -Path (Join-Path $verifyRoot "Program.cs") -Value @"
using System;
using Sqloom.Core.Execution;
using Sqloom.Testing;

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(typeof(ISqloomApplication).FullName);
        Console.WriteLine(typeof(ReplayLaunchOptions).FullName);
    }
}
"@
dotnet build (Join-Path $verifyRoot "SqloomTestingPublicVerify.csproj") --tl:off --nologo "-clp:ErrorsOnly;NoSummary"
$version = (Select-Xml -Path .\Directory.Build.props -XPath '/Project/PropertyGroup/Version').Node.InnerText; if (dotnet tool list --global | Select-String -Pattern '^sqloom\s') { dotnet tool update --global sqloom --version $version } else { dotnet tool install --global sqloom --version $version }; sqloom --version; Get-Command sqloom | Select-Object -ExpandProperty Source
```

If this fails immediately after upload, wait for NuGet indexing to finish and try again.

## 7. If something is wrong after upload

Do not try to reuse the same version number. Fix the repo, bump the version, rerun this runbook, and publish a new package.
