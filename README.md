# Formloom

Formloom is a planned CPU-first, non-neural procedural image-construction application. The first milestone is a deterministic illustrated apple on a wooden table, generated from a bounded prompt grammar and saved as PNG. No neural models, stock images or downloaded textures will be used.

## Current state

The repository currently contains a .NET 10 class-library scaffold, not a runnable desktop application. The implementation checklist is in [`.agents/todo.md`](.agents/todo.md).

- `Formloom.slnx`: existing solution (retain the modern solution format).
- `Formloom/`: initial class-library scaffold.
- `Directory.Build.props` and `.editorconfig`: shared build and formatting conventions.

## Prerequisites

- **.NET 10 SDK**, using the latest available servicing release; a runtime alone is insufficient. The target remains `net10.0` / C# 14.
- **Windows with Microsoft Edge WebView2 Runtime** for the intended PhotinoX desktop host. Native Windows launch, rendering and save acceptance checks must be performed on Windows; WebView2 is not applicable to Linux.
- NuGet connectivity for package restore.

Check the SDK and build the existing scaffold from the repository root:

```sh
dotnet --info
dotnet --list-sdks
dotnet restore Formloom.slnx
dotnet build Formloom.slnx --configuration Debug
dotnet build Formloom.slnx --configuration Release
```

There is not yet a test project or desktop entry point. Test and desktop run commands will be added when those projects exist and have been verified.

## Implementation environment

The initial inspection on 2026-11-02 found Debian 12 (Linux x86-64). `dotnet --info` failed with `dotnet: command not found`; no SDK was found in the standard installation directories checked. Local build/test verification is blocked until a .NET 10 SDK is available; GitHub Actions provides the Windows validation path without installing an SDK in this sandbox. Windows WebView2 and native Windows desktop behavior cannot be verified in this environment.

The selected dependencies remain PhotinoX.Blazor **5.3.5**, SkiaSharp **4.154.0**, and xunit.v3 **4.0.2**. These are planned dependencies, not yet installed or verified. No alternative framework or runtime has been substituted.

## Pull request validation

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every pull request, pushes to `main`, and manual dispatch. It installs .NET 10 on `windows-latest`, restores the solution, builds Debug and Release with warnings treated as errors, and invokes `dotnet test` for each configuration. TRX results are uploaded when available. The current scaffold has no tests, so a successful test command does **not** yet establish test coverage.

The expected PR checks are **Windows / Debug** and **Windows / Release**. A repository administrator must configure both as required status checks in the `main` branch protection/ruleset to enforce them before merging; adding the workflow alone does not prevent merging failed checks. Native window launch, WebView2 integration, visual quality and saving still require manual acceptance testing.

This is a new baseline, not a copy of Tagium CI: inspection of `Vraxo/Tagium` on 2026-11-02 found no `.github/workflows` directory on `main` and zero registered Actions workflows. The workflow has not yet been executed remotely.

## Pull request verification

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) follows the Windows/.NET 10 CI setup in `Vraxo/Lattice`, adapted to `Formloom.slnx`. It runs on every pull request, pushes to `main`, and manual dispatch. Both Debug and Release restore, build with warnings treated as errors, and run `dotnet test`. NuGet packages are cached and TRX test results are uploaded when present.

The current scaffold has no tests, so a green workflow does not yet establish test coverage. Once test projects are added to the solution, the same workflow will run them. Native desktop launch, visual quality and save behavior still require manual acceptance checks.

To enforce passing CI before merging, configure the repository's `main` ruleset/branch protection to require **Windows / Debug** and **Windows / Release** after those checks have run once. A workflow alone does not prevent merging a failing PR.
