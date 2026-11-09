# Phase 10 handoff — red or pink rose in a ceramic vase

## Status and starting point

Phase 10 began from the clean `main` commit `b6fdcde0190958622d214cfd2d5e78b6b2dbe605` after the Phase 9 documentation closeout. Implementation, automated tests, and documentation are on the Arena-tracked branch `arena/03b3bd3a-formloom`; implementation and test source were verified at `c24070cac26ca0848f5535489f1f522c643051cc`. Linux / Release CI run [42590714896](https://github.com/Vraxo/Formloom/actions/runs/42590714896) passed restore, the warnings-as-errors Release build, the full solution test step, and both artifact uploads on review-recording commit `1b771996cb223317ddf617047c6e3c09d50bb35b`. Its procedural-preview artifact is [13660659546](https://github.com/Vraxo/Formloom/actions/runs/42590714896/artifacts/13660659546) (8,551,852 bytes); test results are artifact `13661312248`. Agent-side visual review is complete from the attached PNGs, and timing observations are transcribed from the attached sidecars below. The focused-bundle workflow change passed in run [42592879325](https://github.com/Vraxo/Formloom/actions/runs/42592879325); its `phase10-rose-review` artifact is [13661842949](https://github.com/Vraxo/Formloom/actions/runs/42592879325/artifacts/13661842949) (2,441,169 bytes: a README index plus 24 rose PNG/sidecar files). To avoid searching the full preview archive next time, the existing Linux / Release job now uploads this separate, curated bundle. Focused PR [#55](https://github.com/Vraxo/Formloom/pull/55) is open and must remain open. Merging is outside this phase's authorization.

### Phase 9 prerequisite evidence

- Phase 9 PR #52 merged as `1311f91e582554c2601c0b9476265a1a62ab123d`; post-merge Linux / Release run `42493951766` passed.
- Stem-contact follow-up PR #53 merged as `02286c55c21ed46cc332796c2861b3354ca6dd20`; post-merge Linux / Release run `42507895872` passed.
- Phase 9 documentation closeout PR #54 merged as `b6fdcde0190958622d214cfd2d5e78b6b2dbe605`; post-merge Linux / Release run `42581962836` passed.
- Artifact `13628551050` (`procedural-preview`) supplied the Phase 9 review sheet, ten full-size examples, and timing sidecars. Their review and timing observations are reconciled in `.agents/todo-9.md`, `docs/mixed-vase-arrangement-handoff.md`, and the README; they are not repeated as Phase 10 evidence.

## Scope and prompt grammar

The capability is exactly one red or pink rose growing from one existing blue or cream ceramic vase on a wooden table. The only accepted prompts are:

```text
a red rose in a blue ceramic vase on a wooden table
a red rose in a cream ceramic vase on a wooden table
a pink rose in a blue ceramic vase on a wooden table
a pink rose in a cream ceramic vase on a wooden table
```

The parser uses the existing case-folding, whitespace, and terminal-punctuation normalization, then matches one exact flower → `in` → vase → table-setting grammar. It does not infer colors or quantities, reorder the relationship, accept synonyms, or fall back to an empty vase. Unsupported species/colors, bowls/cups, plural/multiple/bouquet forms, reversed wording, extra objects, adjectives, and incomplete prompts are rejected whole. Roses are not added to the tulip/daisy pair.

The four output subject tokens are `red-rose-in-blue-vase`, `red-rose-in-cream-vase`, `pink-rose-in-blue-vase`, and `pink-rose-in-cream-vase`. At seed 42 the corresponding default save names are `formloom-<subject>-seed-42.png`; the four exact tokens are added to the existing safe-save whitelist.

## Core model and invariants

`RoseColor` is a closed `Red`/`Pink` enum. `VaseRose` is species-specific typed content containing the exact `CeramicInstance` already present in the scene, the rose color, and one `RosePlant`; `StillLifeScene.VaseRose` is mutually exclusive with the standalone tulip, standalone daisy, fixed tulip/daisy arrangement, bowl containment, and cup contents. The vase remains the sole `SceneEntry`. The rose has no independent transform, depth, scene-object sorting, or tabletop/contact shadow. `FlowerKind.Rose` is validated explicitly, while tulip and daisy validation remain species-specific.

`RosePlant` contains one continuous cubic stem, exactly two `RoseLeaf` blades, and one `RoseBloom`. The bloom has a fixed thirteen-petal structure: five outer, five middle, and three inner petals. Petals, leaves, folds, and veins have separate rose-specific geometry records rather than widening tulip/daisy shapes. Validation checks the closed enums, exact counts and layer order, finite/positive dimensions, non-degenerate contour extents, stem attachment, both leaf attachments and opposing directions, vase-opening fit, rim clearance, and normalized safe bounds.

`RoseGeometry` derives its dimensions and placement from the vase profile/body and available headroom; no pixel coordinates or Skia types enter Core. Named streams are `rose.stem`, `rose.leaves`, `rose.petals.outer`, `rose.petals.middle`, and `rose.petals.inner-whorl`. Variation is accepted only within the generation request's existing 0–1 validation and only reshapes the fixed geometry. Rose geometry is intended to remain identical across color, vase glaze, and canvas dimensions and to leave all existing flower streams unchanged.

## Rendering and desktop integration

`RoseMaterial` is in the Skia project and owns separate red and pink petal palettes plus the rose's green stem/leaf tones. `StillLifeRenderer` keeps the existing vase-first path: draw the vase, draw the complete rose stem as one path across the far rim, then leaves and the petals in fixed outer → middle → inner order. The shared tangent-matched below-base continuation is clipped to an inset vase opening so the near lip hides its end. The bloom remains complete in silhouette mode; rose detail omission affects only its folds, edges, veins, and other approved detail. Diagnostic passes remain isolated, and the rose adds no independent cast shadow.

The four rose prompts use the existing `GenerationService` → `DesktopSession` → Skia path, with last-successful preview metadata preserved after rejection. The prompt-help area adds one rose example, and `PngSaveService.KnownSubjects` whitelists the four exact subject tokens without changing earlier names or fallback behavior.

## Tests and artifacts

Added Core tests cover the four prompts, strict rejection/normalization, one-entry/vase identity, safe subject tokens, exclusivity, fixed counts/layers, deterministic geometry, variation/seed bounds, canvas-size independence, color/glaze and existing-flower stream independence, malformed geometry, and species-specific tulip/daisy validation. Renderer tests exercise full-resolution and 512 px pixels for petal layers and inner whorl, both leaves, stem continuity through the opening and near-lip occlusion, no exterior leakage, red/pink distinction, glaze-independent plant pixels, silhouettes, shadow masks, detail omission, diagnostic isolation, antialiasing, and supported output dimensions. Desktop tests exercise all four prompts, preview retention, save metadata/whitelisting, and rendered-component help/input behavior.

The renderer tests write retained files under `artifacts/bin/Formloom.Tests/`, which the existing CI workflow uploads as `procedural-preview`:

- `formloom-vase-rose-canonical-sheet.png` and its `.timing.json` manifest: 1024 × 1024, 2 × 2 cells of 512 × 512, seed 42 / variation 0.35.
- `formloom-vase-rose-robustness-sheet.png` and its `.timing.json` manifest: 2 × 2 cells covering alternate seed, zero variation, and full variation across both colors and both glazes.
- Eight 1024 × 1024 rose PNGs, each with a timing/geometry sidecar; the four canonical PNGs additionally have `-silhouette.png` files.
- `formloom-vase-rose-canonical-silhouette-sheet.png`: an opaque-white 2 × 2 contact sheet of the four full-resolution canonical alpha masks, shown row-major as red/blue, red/cream, pink/blue, pink/cream.

The same Linux / Release job also assembles `phase10-rose-review`: the two scene sheets, the white-backed silhouette sheet, eight full-size renders, four original transparent silhouettes, and ten timing sidecars, plus a short index. The contrasting white preview makes black mask pixels visible in dark-background viewers without replacing the original alpha assets. It complements rather than replaces the comprehensive `procedural-preview`. The reusable selection, validation, upload, and handoff conventions are in [Focused CI review artifacts](review-artifacts.md).

This sandbox has no .NET SDK or alternative C# compiler (`dotnet`, `csc`, `mcs`, `mono`, `msbuild`, `csi`, and `pwsh` are unavailable), so verification ran in GitHub Actions. On tested source/test head `c24070cac26ca0848f5535489f1f522c643051cc`, Linux / Release run `42588356956` passed restore, the Release warnings-as-errors build, the full solution test step, test-result upload, and procedural-preview upload. The review-recording PR-head run `42590714896` on `1b771996cb223317ddf617047c6e3c09d50bb35b` also passed all steps and uploaded preview artifact `13660659546` (8,551,852 bytes) and test-results artifact `13661312248` (158,468 bytes). A download attempt against the prior preview run ended with EOF after redirect to `productionresultssa17.blob.core.windows.net`, outside this sandbox's outbound host allowlist. The user attached the canonical and robustness sheets, all eight 1024 × 1024 PNGs, four canonical silhouettes, and all ten timing sidecars. The images were inspected and the timing measurements from the sidecars are transcribed below. Timings are one-runner observations only, not a performance guarantee or benchmark.

### Timing observations

The attached sidecars are from CI run `42589085858`: .NET 10.0.14 on Ubuntu 24.04.5 LTS with four reported processors. Each full-size measurement is one `StillLifeRenderer.Render` render-and-encode Stopwatch sample; the empty-vase sample is included for context, not as a reliable per-flower cost.

| 1024 × 1024 sample | Seed / variation | Rose render + encode (ms) | Empty-vase render + encode (ms) |
| --- | --- | ---: | ---: |
| Red / blue canonical | 42 / 0.35 | 181.3099 | 176.6916 |
| Red / cream canonical | 42 / 0.35 | 176.0089 | 175.7455 |
| Pink / blue canonical | 42 / 0.35 | 212.9771 | 179.9810 |
| Pink / cream canonical | 42 / 0.35 | 181.7996 | 173.2792 |
| Red / blue alternate seed | 1337 / 0.35 | 187.0674 | 182.8205 |
| Red / cream zero variation | 42 / 0.00 | 181.5532 | 176.0590 |
| Pink / blue full variation | 1337 / 1.00 | 185.1178 | 180.4131 |
| Pink / cream alternate seed | 20261105 / 0.35 | 249.1959 | 208.2737 |

The four canonical 512 × 512 sheet-cell samples, in row-major order (red/blue, red/cream, pink/blue, pink/cream), measured 68.2865, 61.0857, 64.0705, and 61.5634 ms. The robustness sheet's row-major cells (red/blue alternate seed, red/cream zero variation, pink/blue full variation, pink/cream alternate seed) measured 65.9672, 62.1467, 66.3466, and 63.7665 ms. These isolated values are environment-specific observations, not benchmarks or a performance comparison.

## Visual review and limitations

Agent-side visual review covered the canonical 2 × 2 sheet at 512 px per cell, all four matching canonical 1024 × 1024 renders, the robustness sheet and all four robustness renders, and the four supplied canonical silhouette masks. The bloom reads as a compact layered rose rather than a tulip cup or radial daisy; the outer and middle petals and curled inner whorl remain distinct at full size and readable in the review sheet. Red and pink are clearly different at both vase glazes. Both serrated leaves remain visible and attached at separate points, and the upright stem enters the centered vase opening continuously without visible wall leakage. The silhouette masks retain the vase and the whole plant, including the leaves and stem. The vase, rim, setting, glaze, tabletop, lighting, and cast/contact shadow show no rose-specific regression. A small oval mark on the tabletop is also present in the supplied earlier tulip reference, so it is not attributed to the rose. No blocking visual defect was found.

Cosmetic observation only: the bloom is compact relative to the vase and the stem is nearly vertical; the rose remains identifiable at 512 px and its layered structure is clearer at 1024 px. This is not treated as an acceptance defect or a reason to widen the milestone. These are agent-side observations, not separate user acceptance. The timing observations above were transcribed from the supplied `.timing.json` sidecars and remain limited to single-runner measurements. Although direct artifact download in the sandbox was blocked by the external storage redirect, the user-provided PNGs and sidecars enabled the review; the focused bundle makes those assets easier to obtain together for future review.

No native Windows Photino/WebView2 dialog, window-lifecycle, resize, or accessibility behavior is claimed from Linux headless tests. Roses in mixed arrangements, extra rose colors, quantities, bouquets, new vessel profiles, and arbitrary placement are explicitly out of scope.
