# Initial checklist evidence audit

This audit supersedes the earlier blanket closure wording. Delivered features, passing tests and general Windows confirmation do **not** mean that every original acceptance checkbox was exercised. Passing appropriately scoped CI tests are the user's routine gate; this document does not request repeated manual testing.

Baseline: PR #12 passed Linux Release build/tests. The new `DesktopPipelineTests` and extra prompt-snapshot assertions in this change are **authored, not yet executed**. They must not be cited as passing until CI runs.

## Audit of the previously unchecked items

| Checklist location / criterion | Established evidence | Remaining gap / disposition |
| --- | --- | --- |
| 2.5: render default and smaller/larger sizes with correct composition | `RendererProducesDecodablePngAtRequestedSize` runs 256, 1024, 1280 and rectangular sizes; `SceneIsRepeatableAndScalesWithoutChangingComposition` checks unchanged normalized apple geometry | Rendering and size checks complete. Non-default visual composition/scaling review not established; keep that subcheck open. |
| 3.1: PhotinoX 5.3.5 and native assets | Central package version and successful user Windows launch establish the chosen fork and usable Windows assets | No Photino native launch on Linux; do not describe Linux compilation as native-runtime verification. Separate supported Windows evidence from sandbox runtime evidence. |
| 3.1 / 4.2: page/assets load and clean close/relaunch | User confirmed Blazor interaction and integrated Windows app; source copies host assets | Clean shutdown/relaunch not individually evidenced. Keep lifecycle portion open; no new defect reported. |
| 3.2: responsive resizing | CSS grid/media queries exist | No component/browser resize test or application screenshot; source alone does not prove usable layout. Open. |
| 3.3 / 4.2: unsupported prompt and retained image | `RealParserFailureReturnsErrorWithoutPreview`, `FailedPromptRetainsSuccessfulImageAndMetadata`; Razor binds `session.Error` and `session.PreviewUrl` | Implementation and underlying error/state behavior established. Real combined parser→session retention test is newly authored. Actual rendered alert/event wiring is not automatically exercised. |
| 3.3: responsive UI / work off UI thread | `GenerationService` uses `Task.Run`; `PendingGenerationGuardsActionsAndCapturesVisibleInputs` demonstrates a pending generation does not synchronously complete the interaction | Off-thread implementation established; no UI-dispatcher responsiveness/timing assertion. Keep perceived responsiveness subcheck open. |
| 3.3 / 3.5: Randomize selects a visible new seed without secretly generating | `SaveBeforeGenerationIsIgnoredAndRandomizeChangesOnlyInput` verifies changed input and retained image metadata; Razor binds the seed and Randomize method | Selection implementation established. New real-pipeline test covers Randomize followed by rendering; native click/display is not automated. |
| 3.4 / 4.2: saved PNG dimensions and independent viewing | `WritesActualPngAndReplacesExistingFileWithoutTemporaryRemnants` writes actual encoded PNG bytes and independently decodes 64×64 through Skia; renderer tests cover default 1024 output. User supplied external PNG files at 1024×1024 | Write/decode/dimension evidence established. No specifically reported independent desktop viewer check or automated native dialog-to-file roundtrip. Keep those portions open. |
| 3.5: edited prompt, seed, variation use current visible values | Existing pending-generation test captures seed/variation; Razor oninput bindings point to session properties | Added assertions cover edited prompt and its in-flight snapshot (pending CI). No rendered-component event-binding test. |
| 3.5 / 4.2: repeated Generate yields identical output | `IdenticalInputsProduceIdenticalPixels` and Core determinism tests pass | Added real session→service→renderer repeated-call test (pending CI). Two actual native clicks are not automated; do not label the headless test a GUI test. |
| 3.5 / 4.2: increasing variation remains recognizable | `VariationControlsDeviationFromBaseShape`, `VariationKeepsAppleOnTableAndInsideCanvas`, `AllSupportedColorsRenderAtMaximumVariation` pass | Model bounds and valid render evidence complete. Low/high visual comparison and slider event wiring not tested; keep visual/GUI portion open. |
| 3.5: focus, labels, contrast, accessibility | Labels, focus-visible styles and status/alert roles exist in source | No browser/assistive-tech test; no measured contrast assertion. Open, not covered by session tests. |
| 4.1: Debug and Release build | PR #12 Linux Release builds the entire solution with warnings as errors | Debug remains unexecuted. Split Release complete / Debug open; user opted against duplicate Debug PR CI. No runner added. |
| 4.1: exact documented command from clean build | Windows launch confirmed; CI clean checkout/restore/build works on Linux | Exact Windows command and clean local build provenance not recorded. Open as an evidence gap, not a known launch failure. |
| 4.2: default prompt / seed 42 / default variation | Seed-42 PNG supplied, source defaults are 42 / 35%, renderer export uses defaults | Exact session defaults now asserted in the new real-pipeline test (pending CI). No claim that the uploaded file alone proves the variation setting. |
| 4.3: running app screenshot and generated image | Three user-supplied generated PNGs were visually reviewed; filenames/seeds recorded in handoff | Generated-output evidence complete; no full app screenshot supplied/captured. Keep screenshot portion open, not the entire image-evidence task. |

## Changes made by this audit

- Marked stale implementation checkboxes for parser-error presentation/retention and seed selection using existing tests plus actual Razor bindings, not invented manual observations.
- Split multi-size rendering, CPU offloading, file decoding, Debug/Release and screenshot criteria into independently evidenced subchecks. Parent items remain unchecked wherever the original combined criterion is not established.
- Added real pipeline tests for default output/repeated generation, unsupported-input retention and Randomize→Generate, plus prompt snapshot assertions. These tests run through DesktopSession and the actual generation service/renderer, not a fake bitmap producer; they do not launch Photino.
- Kept component rendering/event wiring, native-dialog behavior, lifecycle, resize/accessibility and visual comparisons explicitly unverified. General user approval is not relabeled as per-case execution or a waiver of every historical item.

## Boundary of the routine gate

Run solution-wide Linux Release CI and add regression coverage for changed behavior. Native/UI risks should be disclosed when affected, rather than requesting a full manual pass every time or claiming tests cover everything. No feature expansion is included in this audit. The initial acceptance checklist remains partially verified until its remaining criteria are evidenced or explicitly revised/waived; it is not fully complete just because feature implementation is delivered.
