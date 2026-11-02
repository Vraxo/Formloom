# Formloom

Formloom is a CPU-first, non-neural procedural image-construction project. Its first planned milestone is a deterministic illustrated apple on a wooden table, generated from a bounded prompt grammar and saved as PNG. No neural models, stock images or downloaded textures will be used.

## Current structure

- `Formloom.slnx`: solution, retaining the existing modern solution format.
- `Formloom.Core`: renderer-independent generation library.
- `Formloom.Rendering.Skia`: CPU raster renderer library, referencing Core.
- `Formloom.Desktop`: Razor library scaffold with PhotinoX.Blazor, referencing Core and Rendering.Skia. The executable host and UI are not implemented yet.
- `Formloom.Tests`: xUnit v3 test project, referencing Core and Rendering.Skia. The initial smoke test loads Core and checks for forbidden rendering/UI dependencies.
- `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`: shared build, centrally managed dependencies and formatting.
- [`.agents/todo.md`](.agents/todo.md): implementation checklist and acceptance criteria.

Core now supports `a red apple on a wooden table`, with `green` or `yellow` in place of `red`. Parsing is case-insensitive, collapses whitespace and accepts terminal `.`, `!` or `?`. Unknown words, materials and relationships are rejected rather than ignored.

`SceneGenerator.Generate(new GenerationRequest(prompt, Seed: 42, Variation: 0.35))` returns either a normalized, immutable scene or a user-facing error. Dimensions must be 64–4096 on each axis; variation must be finite and within 0–1. Defaults are 1024 × 1024. PCG32 with stable UTF-8 FNV-1a named streams isolates shape, stem, leaf, surface, grain and background randomness. Variation scales bounded offsets; zero variation uses the base composition. Coordinates and proportions are independent of output resolution.

Core tests cover parsing, invalid input, a PCG reference sequence, repeatability, independent streams, helper bounds and scene variation. Execution of the new tests is pending CI. Procedural rendering, desktop launch and saving are not implemented.

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
dotnet test --solution Formloom.slnx --configuration Release --no-build --report-trx --results-directory TestResults
```

There is no desktop run command yet because the desktop project is not an executable host.

## Dependencies

Versions are declared once in `Directory.Packages.props`:

| Package | Version | Purpose |
| --- | --- | --- |
| PhotinoX.Blazor | 5.3.5 | Planned native Blazor host |
| SkiaSharp | 4.154.0 | CPU raster rendering |
| xunit.v3 | 4.0.2 | Tests |
| Microsoft.Testing.Extensions.TrxReport | 2.5.1 | Native Microsoft.Testing.Platform TRX reports |
| NewStyleCop.Analyzers | 1.2.1 | Existing repository analyzer |

`global.json` opts .NET 10 into the native Microsoft.Testing.Platform test runner. Tests use `--solution` and `--report-trx`, not VSTest’s positional solution and `--logger` syntax. The VSTest SDK and adapter are not required. No chosen framework has been substituted. Native assets and desktop startup still need runtime validation.

## Pull request verification

[`.github/workflows/ci.yml`](.github/workflows/ci.yml) runs on every pull request, pushes to `main`, and manual dispatch. A single `ubuntu-latest` job installs .NET 10, restores the entire solution, builds Release with warnings treated as errors, and runs `dotnet test`. NuGet packages are cached and TRX results are uploaded when present. Debug is available locally but is not duplicated in PR CI.

Require **Linux / Release** in the `main` ruleset/branch protection to enforce passing CI before merging. Remove the previous **Windows / Debug** and **Windows / Release** requirements if configured, or they will block merging while waiting for checks that no longer run. A workflow alone does not prevent merging a failing PR.

Linux CI validates managed compilation and tests, not the Windows desktop runtime. Native window launch, WebView2 integration, visual quality and PNG saving still require Windows acceptance testing. Future Skia rendering tests must include compatible Linux native assets; the current smoke test does not invoke Skia or launch Photino.

The agent environment is Debian 12 / Linux x86-64 without a .NET SDK. Per project direction, no local SDK is installed; GitHub Actions is the build/test validation path. The Ubuntu workflow remains unverified until CI runs. The previous Windows scaffold jobs failed because Microsoft.Testing.Platform rejected the VSTest path on .NET 10. The native runner configuration addresses that reported error; CI must confirm the fix.
