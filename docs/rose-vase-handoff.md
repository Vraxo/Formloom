# Phase 10 handoff — red or pink rose in a ceramic vase

## Status and starting point

Phase 10 began from the clean `main` commit `b6fdcde0190958622d214cfd2d5e78b6b2dbe605` after the Phase 9 documentation closeout. Implementation, automated tests, and documentation are on the Arena-tracked branch `arena/03b3bd3a-formloom`; implementation and test source were verified at `c24070cac26ca0848f5535489f1f522c643051cc`. Linux / Release CI run [42588356956](https://github.com/Vraxo/Formloom/actions/runs/42588356956) passed restore, the warnings-as-errors Release build, the full solution test step, and both artifact uploads. The procedural-preview artifact is [13660855029](https://github.com/Vraxo/Formloom/actions/runs/42588356956/artifacts/13660855029) (8,551,859 bytes); test results are artifact `13659978125`. Focused PR [#55](https://github.com/Vraxo/Formloom/pull/55) is open and must remain open. Visual review and timing transcription remain pending because the sandbox cannot retrieve the uploaded preview through its external storage redirect; this is the outstanding acceptance gate. Merging is outside this phase's authorization.

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

This sandbox has no .NET SDK or alternative C# compiler (`dotnet`, `csc`, `mcs`, `mono`, `msbuild`, `csi`, and `pwsh` are unavailable), so verification ran in GitHub Actions. On tested source/test head `c24070cac26ca0848f5535489f1f522c643051cc`, Linux / Release run `42588356956` passed restore, the Release warnings-as-errors build, the full solution test step, test-result upload, and procedural-preview upload. The preview artifact `13660855029` is 8,551,859 bytes; the test-results artifact is `13659978125` (158,525 bytes). Downloading the preview with `gh run download 42588356956 --name procedural-preview` failed with EOF after redirect to `productionresultssa17.blob.core.windows.net`, which is outside this sandbox's outbound host allowlist. Consequently the images and timing/geometry sidecars are uploaded but could not be inspected or transcribed here. Timing values remain unreported; when available, report only environment-specific observations, not a performance guarantee or benchmark.

## Visual review and limitations

Visual review remains pending, not because CI failed to generate the files but because its artifact download redirects to a storage host this sandbox cannot access. The uploaded `procedural-preview` artifact is available from [run 42588356956, artifact 13660855029](https://github.com/Vraxo/Formloom/actions/runs/42588356956/artifacts/13660855029). To close this gate, inspect all four canonical 512 px cells and matching 1024 px samples, plus all four robustness cells and samples. In particular, check that the bloom reads as a layered cupped rose (not a tulip or radial daisy), the three petal layers and curled whorl remain distinguishable, red and pink differ clearly, both leaves stay visible and attached, and the stem enters continuously and is hidden by the near lip. Also check the vase, setting, glaze, shadow, and canvas margins for regressions. Read the timing/geometry sidecars and record only environment-specific observations. No such visual or timing observations are claimed here; the downloadable artifact must be provided in this conversation if agent-side inspection is required. Record cosmetic observations separately from defects; assistant-side inspection is not user acceptance.

No native Windows Photino/WebView2 dialog, window-lifecycle, resize, or accessibility behavior is claimed from Linux headless tests. Roses in mixed arrangements, extra rose colors, quantities, bouquets, new vessel profiles, and arbitrary placement are explicitly out of scope.
