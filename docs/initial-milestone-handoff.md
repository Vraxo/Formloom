# Initial milestone handoff

**Review checkpoint: 2026-11-02.** The first vertical slice is implemented and working according to user Windows feedback, Linux Release CI and reviewed generated PNGs. This is a handoff for review, not a claim that every manual acceptance case was exercised. No new subjects, editor features or broader grammar are included or authorized here.

## Delivered

- Native PhotinoX.Blazor desktop host with prompt, editable seed, Randomize, variation, Generate, preview and asynchronous native Save PNG.
- Strict red/green/yellow apple-on-wooden-table grammar; errors rather than silent interpretation of unsupported text.
- Immutable normalized scene description, PCG32 and stable UTF-8 FNV-1a named streams. Same inputs and build/runtime/platform reproduce the same pixels; different renderer builds may intentionally change them.
- Procedural CPU Skia background, tabletop, seeded grain and knot, organic apple, leaf, tapered stem, shading and cast/contact shadows. No artwork, textures, neural models or network calls are used in generation.
- Background rendering with duplicate-action guards, retention of the last successful image after errors, and image-specific save metadata. Saving writes PNG bytes, not a UI screenshot.
- Opacity regression fixed in PR #8, contact/stem refinement in PR #9, wood grain refinement in PR #10.

## Project boundaries and packages

All projects are top-level folders. Core has no Skia or UI dependency; Rendering.Skia references Core; Desktop references both; Tests references Core and Rendering.Skia only. Desktop services handle orchestration and native saving; components collect input and present state.

All projects target .NET 10 / C# 14. Central package versions remain PhotinoX.Blazor **5.3.5**, SkiaSharp **4.154.0**, xunit.v3 **4.0.2**, with Linux native assets **4.154.0**, TRX reporting **2.5.1** and the existing NewStyleCop analyzer **1.2.1**. No selected framework was substituted. `global.json` opts into Microsoft.Testing.Platform, replacing the incompatible VSTest path on .NET 10.

The retained solution format is `.slnx`, and the flat project layout and Release-only Linux CI are user-approved deviations from the initial organizational/verification plan. They do not change the Windows desktop target.

## Verification evidence

| Area | Evidence | Boundary |
| --- | --- | --- |
| Latest implementation build/test | [PR #10 CI](https://github.com/Vraxo/Formloom/actions/runs/41492552181): Linux / Release passed in 36 seconds | Restore, full solution Release build with warnings as errors, Core/renderer tests and artifact export; not a Windows GUI test |
| Core | Parser/validation tests, PCG reference sequence, range helpers, stream isolation, scene repeatability and bounded variation | Automated tests; UI edge cases are separate |
| Renderer | PNG signature/decoding, 256/1024/1280 square and rectangular sizes, pixel repeatability, seed differences, opacity, contact shadows and table-region variation | No golden-image snapshots; visual scaling at non-default sizes not manually reviewed |
| Windows host | User confirmed Blazor interaction, then the integrated app working | Does not establish every close/relaunch, save-failure or accessibility case |
| Visual output | User supplied seed 42, 17512344472955961833 and 4003733692769532287 PNGs; header checks confirm all three are 1024 × 1024 | Different seeds/builds; not controlled same-seed before/after comparisons |

Visual review found a recognizable opaque apple, organic silhouette, distinct materials, coherent upper-left lighting, improved stem attachment/contact and calmer wood grain. The remaining stylization, including a small dark knot, was judged suitable for the first checkpoint rather than grounds for an indefinite polish loop. This is not photorealism or a general-purpose image generator.

The PNGs were supplied in the conversation, not checked into the repository or used as rendering inputs. No screenshot of the full running application was supplied or captured. The sandbox cannot launch/inspect the Windows GUI, and redirected CI artifact storage is inaccessible here. User uploads enabled visual output review despite that limitation.

## Reproduce and save

On Windows, install the latest .NET 10 SDK servicing release and WebView2 Runtime. From the repository root:

```sh
dotnet restore Formloom.slnx
dotnet build Formloom.slnx --configuration Release --no-restore
dotnet test --solution Formloom.slnx --configuration Release --no-build --report-trx --results-directory TestResults
dotnet run --project src/Formloom.Desktop/Formloom.Desktop.csproj --configuration Release
```

Use `a red apple on a wooden table`, seed `42`, variation `35%`. Save PNG opens a native dialog; the selected path is the output location. The suggested filename uses the successful image's seed, even after ungenerated control edits. Output is 1024 × 1024.

The renderer export test writes `formloom-apple-seed-42.png` beside the test assembly under `artifacts/bin/Formloom.Tests/`. CI uploads it as **procedural-preview**. These artifacts are not committed to Git.

## Still individually unverified

- Debug build: intentionally absent from PR CI; run `dotnet build Formloom.slnx --configuration Debug` once if full original Debug/Release acceptance is required. No local SDK was installed in the agent sandbox.
- Clean-build launch using the exact documented command, then close/relaunch without lifecycle errors.
- UI same-input repeated clicks, immediate edits before Generate, and Randomize action behavior. Core/renderer determinism and different-seed outputs are already covered, but those do not prove UI state handling.
- Low/high variation and all color options exercised in the Windows UI. Automated coverage exists for the core/renderer.
- Unsupported prompt and invalid seed messages with previous-image retention; duplicate-click behavior and perceived UI responsiveness.
- Save cancellation/failure, successful-image metadata after control edits, and opening the saved file in an independent image viewer. Uploaded files confirm output exists and has PNG dimensions, not that all save paths were tested.
- Window resizing, keyboard focus/contrast/basic accessibility, and a screenshot of the running app.
- Visual inspection of smaller/larger renders and a controlled same-seed comparison across the polish changes.

These are verification gaps, not newly reproduced defects. See [the checklist](../.agents/todo.md) for task-level status. Stop at this checkpoint: choose further priorities only after review; do not infer authorization for more subjects, a scene editor or an expanded language system.

## Post-handoff verification: reliability follow-up

PR #12 merged after [Linux Release CI](https://github.com/Vraxo/Formloom/actions/runs/41499515371) passed in 33 seconds. Headless Desktop.Tests now covers session guards, image retention, metadata snapshots, failure recovery, real generation-service results and filesystem save/replacement paths. Core/renderer test references remain unchanged. The user confirmed the refactored Windows application works and chose passing, appropriately scoped tests as the routine verification gate instead of repeated manual smoke checks.

The historical unverified items above remain accurate evidence limits, not recurring requests for user testing. The smoke checklist is available when troubleshooting native/UI changes. No claim is made that native dialogs, Razor event wiring, visual appearance or every OS failure mode is covered by the automated suite. The reliability implementation passed CI, but that does not close every original acceptance criterion. A subsequent feature milestone must be explicitly selected; see the audit below before treating the checklist as complete.

## Acceptance audit correction

The earlier closure wording conflated delivered implementation with full checklist verification. The [checklist evidence audit](checklist-audit.md) now maps all previously unchecked criteria to actual evidence and remaining gaps, splits partially fulfilled tasks, and identifies newly authored real-pipeline tests (not yet executed). Historical individual native/UI checks are neither asserted as run nor silently waived. Passing CI is the routine gate; repeated manual smoke checks are not required for every change, but automation boundaries remain explicit.

## Final acceptance scope (approved; execution pending)

After reviewing the remaining checkboxes, the user approved rendered-component coverage, one-off Debug verification and explicit deferral of the native/manual checks. The [checklist D1–D6 table](../.agents/todo.md) records the precise deferrals; these are not claims of manual execution. Actual component events/markup are exercised by new bUnit tests, alongside the real pipeline tests that passed PR #13. Component tests plus the DI adjustment have not run yet. Debug can now be requested through the optional `verify_debug` manual workflow input, but has not yet been executed.

Milestone closure under this revised scope requires both execution gates to pass. The full original manual criteria are not all established. Routine PR CI remains one Linux Release job, and no composition controls or other feature expansion are authorized here.

## Final verification result (supersedes earlier pending statements)

Both remaining execution gates are satisfied:

- [PR #14 Release CI](https://github.com/Vraxo/Formloom/actions/runs/41511913612) passed, including rendered-component tests, in 46 seconds.
- The user reported a successful Windows Debug build of all five projects in 10.5 seconds, using `dotnet build Formloom.slnx --configuration Debug --warnaserror -p:RestoreSources=https://api.nuget.org/v3/index.json`. This was a local build, not a Debug workflow run. The dispatch attempt received HTTP 403; no successful Debug run URL is claimed.

The command-scoped source override addressed the user's inherited multiple-feed NU1507 warning without changing global or repository feed policy. The proposed repository NuGet configuration was removed before commit.

The initial milestone is complete under the revised acceptance scope. Earlier audit entries describe historical evidence gaps; new component coverage and the Debug result supersede those pending gates. D1–D6 native/manual deferrals remain explicit and are not recast as executed checks. Routine CI remains Linux Release-only. No further feature scope is selected by this closure.
