# Formloom

Formloom is a CPU-first, non-neural procedural image-construction project. Its first planned milestone is a deterministic illustrated apple on a wooden table, generated from a bounded prompt grammar and saved as PNG. No neural models, stock images or downloaded textures will be used.

## Current structure

- `Formloom.slnx`: solution, retaining the existing modern solution format.
- `src/Formloom.Core`: renderer-independent generation library.
- `src/Formloom.Rendering.Skia`: CPU raster renderer library, referencing Core.
- `src/Formloom.Desktop`: Razor library scaffold with PhotinoX.Blazor, referencing Core and Rendering.Skia. The executable host and UI are not implemented yet.
- `tests/Formloom.Tests`: xUnit v3 test project, referencing Core and Rendering.Skia. The initial smoke test loads Core and checks for forbidden rendering/UI dependencies.
- `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`: shared build, centrally managed dependencies and formatting.
- [`.agents/todo.md`](.agents/todo.md): implementation checklist and acceptance criteria.

The projects are foundations only: prompt parsing, procedural rendering, desktop launch and saving are not implemented.

## Prerequisites and commands

- **.NET 10 SDK**, using the latest installed servicing release; all projects inherit `net10.0`, nullable reference types and implicit usings. The default language version is C# 14.
- NuGet connectivity for restore.
- **Windows with Microsoft Edge WebView2 Runtime** for the intended PhotinoX desktop host. Native desktop acceptance must be performed on Windows; WebView2 is not applicable to Linux.

From the repository root:

```sh
dotnet --info
dotnet restore Formloom.slnx
dotnet build Formloom.slnx --configuration Debug --no-restore
dotnet build Formloom.slnx --configuration Release --no-restore
dotnet test Formloom.slnx --configuration Release --no-build --logger trx --results-directory TestResults
```

There is no desktop run command yet because the desktop project is not an executable host.

## Dependencies

Versions are declared once in `Directory.Packages.props`:

| Package | Version | Purpose |
| --- | --- | --- |
| PhotinoX.Blazor | 5.3.5 | Planned native Blazor host |
| SkiaSharp | 4.154.0 | CPU raster rendering |
| xunit.v3 | 4.0.2 | Tests |
| Microsoft.NET.Test.Sdk | 17.14.1 | VSTest integration |
| xunit.runner.visualstudio | 3.1.4 | xUnit discovery through `dotnet test` |
| NewStyleCop.Analyzers | 1.2.1 | Existing repository analyzer |

The VSTest SDK and adapter preserve the existing CI `dotnet test`/TRX path while using xUnit v3. No chosen framework has been substituted. Native assets and desktop startup still need runtime validation.

## Pull request verification

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) follows the Windows/.NET 10 CI setup in `Vraxo/Lattice`, adapted to `Formloom.slnx`. It runs on every pull request, pushes to `main`, and manual dispatch. Both Debug and Release restore, build with warnings treated as errors, and run `dotnet test`. NuGet packages are cached and TRX test results are uploaded when present.

Require **Windows / Debug** and **Windows / Release** in the `main` ruleset/branch protection to enforce passing CI before merging. A workflow alone does not prevent merging a failing PR. Native window launch, visual quality and PNG saving require separate manual acceptance checks.

The agent environment is Debian 12 / Linux x86-64 without a .NET SDK. Per project direction, no local SDK is installed; GitHub Actions is the build/test validation path. The new project scaffolding and smoke test remain unverified until CI runs; a green result for the earlier single-library scaffold does not validate these changes.
