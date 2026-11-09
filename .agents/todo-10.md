# Phase 10 — A Red or Pink Rose in a Ceramic Vase

> **Status:** Implementation, automated verification, and agent-side visual review are complete. Linux / Release CI passed on review-recording commit `1b771996cb223317ddf617047c6e3c09d50bb35b` in run `42590714896`; preview artifact `13660659546` was uploaded. The canonical and robustness sheets, eight full-size renders, and four silhouettes were inspected from the user-provided attachments; no blocking visual issue was found. Timing sidecars are now transcribed in the handoff as single-runner observations, not benchmarks. To avoid searching the 86+ file preview archive, the Linux / Release workflow now publishes a focused `phase10-rose-review` artifact alongside the comprehensive `procedural-preview`. PR [#55](https://github.com/Vraxo/Formloom/pull/55) is open; keep it open and do not merge.

## Goal

Add one recognisable red or pink rose growing from one existing blue or cream ceramic vase. The rose is a new species with its own typed Core model, normalized geometry and Skia material. Its bloom must read as a layered rose, not as a recoloured tulip or a daisy with a different centre.

The four supported prompts are:

1. a red rose in a blue ceramic vase on a wooden table
2. a red rose in a cream ceramic vase on a wooden table
3. a pink rose in a blue ceramic vase on a wooden table
4. a pink rose in a cream ceramic vase on a wooden table

Exactly one rose, one stem, and two leaves. The existing vase remains the only scene entry. This phase adds a standalone rose prompt only; it does not add roses to the existing tulip/daisy pair.

## Phase 9 prerequisite — verified evidence and required documentation reconciliation

Phase 9's follow-up was merged after its checklist and handoff were written. The latest verified main commit at the time this specification was authored was `02286c55c21ed46cc332796c2861b3354ca6dd20`. Since then, PR #54 merged the Phase 9 documentation closeout as `b6fdcde0190958622d214cfd2d5e78b6b2dbe605`; post-merge Linux / Release run [42581962836](https://github.com/Vraxo/Formloom/actions/runs/42581962836) passed. Phase 10 started from that exact latest-main commit with a clean working tree.

- Phase 9 implementation PR [#52](https://github.com/Vraxo/Formloom/pull/52) merged as `1311f91e582554c2601c0b9476265a1a62ab123d`; its post-merge Linux Release run [42493951766](https://github.com/Vraxo/Formloom/actions/runs/42493951766) passed.
- Vase stem-contact follow-up PR [#53](https://github.com/Vraxo/Formloom/pull/53) merged as `02286c55c21ed46cc332796c2861b3354ca6dd20`; its post-merge Linux Release run [42507895872](https://github.com/Vraxo/Formloom/actions/runs/42507895872) passed.
- The PR #53 workflow artifact is artifact ID `13628551050` (`procedural-preview`). It contains the 1280 × 960 Phase 9 review sheet, ten 1024 × 1024 mixed-arrangement samples, their individual timing sidecars, and the sheet timing/manifest sidecar.
- The sheet is 4 columns × 3 rows with 320 × 320 cells. Its first row is the four red/yellow × blue/cream canonical combinations; the remaining six cells show alternate seeds and variation extremes.
- Artifact review performed on 2026-11-09: assistant-side inspection covered all ten 320 × 320 sheet cells and all ten user-supplied 1024 × 1024 cases (the four canonical combinations, two alternate seeds, and four variation cases). The two blooms remain distinguishable, all four leaves are visible, both stems enter the vase continuously, and the near lip hides the interior continuations. No blocking visual defect was found. This records agent-side artifact inspection, not a claim of separate user acceptance.
- All ten 1024 × 1024 timing sidecars were read. Their observed render-and-encode values range from approximately 132.8 ms to 192.4 ms; the four canonical cases range from approximately 132.8 ms to 137.6 ms. The 320 × 320 sheet cells range from approximately 31.4 ms to 45.6 ms. These are observations from .NET 10.0.14 on Ubuntu 24.04.5 LTS with four reported processors, not performance guarantees or a universal cost model.

Those statements described the repository before PR #54. They are now reconciled in the current Phase 9 checklist, README, and handoff: PR #53's merge and post-merge CI are recorded, all Phase 9 checklist items are checked, visual scope is distinguished from user acceptance, and the supplied timing sidecars are transcribed. The current Phase 10 starting point also includes the post-merge PR #54 documentation closeout; no Phase 9 source, visual, or CI discrepancy was found.

## Sequential implementation checklist

### 0. Verify the starting point and close the Phase 9 record

- [x] Confirm that Phase 9 PR #52 merged as `1311f91e582554c2601c0b9476265a1a62ab123d` and its post-merge Linux Release run `42493951766` succeeded.
- [x] Confirm that Phase 9 follow-up PR #53 merged as `02286c55c21ed46cc332796c2861b3354ca6dd20` and post-merge run `42507895872` succeeded.
- [x] Confirm that artifact `13628551050` contains the Phase 9 4 × 3 sheet, ten full-resolution mixed-arrangement images, ten individual timing sidecars, and the sheet timing sidecar.
- [x] Inspect the ten 320 px review-sheet cells and the four canonical 1024 px samples; inspect representative high-variation samples. No blocking visual issue was found.
- [x] Read the ten full-resolution timing sidecars and the sheet timing sidecar; record their values and environment-specific limitations above.
- [x] Start from the latest `main`, record its exact HEAD, and confirm that the working tree/branch has no unrelated changes. Phase 10 started from clean `b6fdcde0190958622d214cfd2d5e78b6b2dbe605`; do not reset to the older `02286c5` prerequisite commit.
- [x] Re-read `.agents/todo-9.md` and `docs/mixed-vase-arrangement-handoff.md` at the current main commit; verify them against merged source, PR #53 run `42507895872`, artifact `13628551050`, and the PR #54 closeout.
- [x] Reconcile stale PR #53 unmerged/pending language: PR #54 records merge commit `b6fdcde` and successful post-merge main run `42581962836`.
- [x] Reconcile Phase 9 review/timing checkboxes with the evidence. All Phase 9 checklist items are checked; the full-size review/timing record is present and retains the single-observation limitations.
- [x] Record Phase 9 visual review as assistant-side artifact inspection, not separate user acceptance. The assistant inspected all ten 320 × 320 sheet cells and all ten user-supplied 1024 × 1024 samples; the handoff now states that scope explicitly.
- [x] Preserve measured timing as environment-specific observations: sheet cells 31.3668–45.6050 ms and full-size cases 132.8377–192.4158 ms. No performance direction or per-flower cost is inferred from one sample per case.
- [x] Inspect repository advances beyond the spec's recorded `02286c5`: PR #54 adds the Phase 9 documentation closeout at `b6fdcde`; its latest main Linux / Release run `42581962836` passed. No later application-source change was found.
- [x] Compare Phase 9 source, visual evidence, and CI with the verified record; no material contradiction or blocking defect was found.
- [x] Confirm `.agents/todo-10.md` is the only Phase 10 milestone specification; preserve all earlier checklists and handoffs.

**Phase 9 prerequisite gate:** continue only after the two Phase 9 documents accurately describe the merged and reviewed state, and the current `main` has passed its latest required Release CI. A newly discovered blocking issue must be resolved or reported before rose implementation begins.

### 1. Inspect the current architecture and define the narrow rose model

- [x] Read the current Core, renderer, parser, scene validation, desktop generation/save, and test implementations before editing.
- [x] Trace the four existing vase-content paths: standalone tulip, standalone daisy, the fixed tulip/daisy pair, and an empty vase.
- [x] Trace the complete prompt → Core → scene generation → validation → Skia render → DesktopSession → PNG save path for an existing standalone flower.
- [x] Inspect the current `FlowerKind`, `VaseMouth`, `VaseStem` projection/helpers, and shared vase-first plant draw order in the current source. Names and paths in this document are orientation only.
- [x] Confirm how red/yellow tulip and white/golden daisy appearances are represented and validated. Do not widen those models to accommodate rose-specific data.
- [x] Confirm how the renderer carries a complete stem through the far rim and clips its tangent-matched interior continuation behind the near lip.
- [x] Confirm how flower geometry, vase profile, transforms, bounds, variation, and named random streams are represented.
- [x] Choose a species-specific `RosePlant` model and its small set of supporting geometry records. Keep rose petal geometry separate from `TulipPetal` and `DaisyPetal`.
- [x] Add a closed `RoseColor` representation with exactly `Red` and `Pink`, or an equally narrow closed appearance representation if the existing conventions make that more coherent.
- [x] Add a typed `VaseRose` vase-content record that refers to the exact vase instance already held by the scene.
- [x] Add `FlowerKind.Rose` and update all exhaustive validation/dispatch sites deliberately. Do not make `FlowerKind` a stringly typed extension point.
- [x] Keep the vase as the only `SceneEntry`; the rose is vase content, not an independently transformed scene object with its own depth or shadow.
- [x] Make rose contents mutually exclusive with tulip-only, daisy-only, tulip/daisy pair, bowl containment, and cup contents.
- [x] Keep Core independent of SkiaSharp, pixel coordinates, bitmap/native resources, and Desktop/UI types.
- [x] Avoid broad nullable flower records, arbitrary flower arrays, a general plant registry, a generic bouquet model, reflection dispatch, or a universal contents abstraction.
- [x] Record the chosen typed model and exact invariants in the Phase 10 handoff; implementation verification, visual review, and timing transcription are recorded.

### 2. Add exact rose prompt grammar and save identity

- [x] Add a rose prompt value type following the current prompt-model conventions.
- [x] Support exactly the four prompts listed at the top of this document: red/pink rose × blue/cream vase.
- [x] Use exact grammar: `a red rose` or `a pink rose`, followed by `in`, exactly one existing blue/cream ceramic vase descriptor, and `on a wooden table`.
- [x] Preserve the current case normalization, repeated-whitespace normalization, and terminal-punctuation handling.
- [x] Preserve the existing strict parsing contract. Do not introduce synonyms, flexible word order, plural inference, adjective interpretation, or natural-language fallbacks.
- [x] Ensure rose parsing is attempted before any broader parser branch that could incorrectly accept or degrade a malformed rose prompt.
- [x] Reject an unspecified rose, white/yellow/blue/purple rose, alternate flower color, alternate vase glaze, and every unsupported species.
- [x] Reject roses in bowls/cups, roses beside a vase, vase-first phrasing, reversed relationships, missing table phrases, extra scene objects, and malformed separators.
- [x] Reject plural roses, two roses, bouquets, rose/tulip or rose/daisy mixtures, and any third flower.
- [x] Verify that each invalid prompt is rejected as a whole and never falls back to an empty vase, a tulip, or a daisy.
- [x] Add parser tests for all four supported prompts and each established normalization behavior.
- [x] Add a focused negative-prompt matrix for unsupported color, species, order, quantity, container, relationship, and extra-object variants.
- [x] Preserve every pre-existing prompt, parser result shape, rejection behavior, and subject token.

Expected subject tokens and default seed-42 save names:

| Prompt | Subject token | Default filename |
| --- | --- | --- |
| red rose / blue vase | `red-rose-in-blue-vase` | `formloom-red-rose-in-blue-vase-seed-42.png` |
| red rose / cream vase | `red-rose-in-cream-vase` | `formloom-red-rose-in-cream-vase-seed-42.png` |
| pink rose / blue vase | `pink-rose-in-blue-vase` | `formloom-pink-rose-in-blue-vase-seed-42.png` |
| pink rose / cream vase | `pink-rose-in-cream-vase` | `formloom-pink-rose-in-cream-vase-seed-42.png` |

- [x] Add exactly these four new subject tokens to the existing safe-save whitelist.
- [x] Verify seed, variation, extension, filename normalization, and whitelist fallback behavior.
- [x] Verify that rejected prompts retain the last successful preview and its associated metadata.

### 3. Construct a recognisable rose from normalized geometry

- [x] Add a dedicated `RoseGeometry` generator and species-specific records such as `RoseBloom`, `RosePetal`, `RoseLeaf`, and `RosePlant`; use separate files for primary types in keeping with repository conventions.
- [x] Make every rose contour and attachment coordinate normalized and derived from the existing vase profile/body, never from fixed 1024 px placement constants.
- [x] Derive the stem base from the same `VaseMouth` opening helpers used by the existing flower species.
- [x] Derive the bloom size, height, and top margin from the vase opening and available headroom; do not let the rose collide with the vase rim or canvas edge.
- [x] Give the rose a compact, layered, cupped silhouette with overlapping outer, middle, and inner petal layers.
- [x] Shape the central whorl as a visibly tighter, partially curled arrangement; do not add a daisy-like centre disk.
- [x] Make petal overlap and depth order deliberate so roots are hidden where appropriate and the inner whorl remains readable.
- [x] Use visibly curved petal edges and bounded asymmetry; avoid a stack of identical circles, flat radial spokes, or a single scalloped blob.
- [x] Keep the outer petal silhouette broad enough to read as a rose while preserving enough negative space and contour separation to distinguish individual layers.
- [x] Ensure the bloom is visibly different from the tulip's five-part cupped shape and the daisy's twelve-petal radial head.
- [x] Choose and document a small fixed petal/layer count suitable for the rendered size. Keep counts stable; variation may reshape petals but must not randomly add/remove them.
- [x] Derive petal widths, lengths, curvature, overlap, and layer radii from normalized rose dimensions rather than canvas pixels.
- [x] Add restrained directional shading and fold contours in the geometry/material boundary without embedding RGB values in Core.
- [x] No calyx/sepals were added; the implementation keeps the plant model narrow. Revisit only if visual review finds a bloom-to-stem transition defect.
- [x] Create exactly one continuous stem with a bounded natural curve and a thickness proportionate to the vase opening and bloom.
- [x] Create exactly two leaf blades attached to the stem at separate heights. Keep their attachment points and direction deterministic and their silhouettes visible at normal viewing size.
- [x] Give rose leaves a restrained species-appropriate shape, including a clear central vein and optional subtle serration if it improves recognition without creating noisy edges.
- [x] Ensure leaves do not merge into the bloom, each other, or the vase rim; both must remain visibly attached to the stem.
- [x] Measure `RosePlant.Bounds` from the actual bloom, stem, calyx/sepals if present, and both leaf contours. Do not accept caller-supplied bounds that can disagree with drawn geometry.
- [x] Use stable, independently named random streams for rose stem, leaves, and petal/layer detail. Do not use `GetHashCode()` or mutable shared PRNG order to derive seed identity.
- [x] Keep variation bounded and continuous from 0 to 1; it may alter contour, rotation, curvature, and offsets but must not break the rose's identity or fixed geometry counts.
- [x] Prove identical prompt/seed/variation inputs produce identical Core geometry and that different seeds create visible, coherent changes.
- [x] Prove rose randomness does not perturb existing tulip, daisy, or tulip/daisy-pair geometry.
- [x] Prove changing only the vase glaze does not change the rose's geometry.
- [x] Test normalized bounds and vase-relative clearances across representative seeds, variation extremes, and all supported canvas aspect ratios.
- [x] Keep the rose composition centred and visually balanced above the vase; do not introduce a user-controlled layout or arbitrary flower-placement mechanism.

### 4. Integrate the rose with the existing vase-content path

- [x] Add `VaseRose.Create` or the equivalent focused factory using the existing vase profile and normalized geometry conventions.
- [x] Preserve the exact vase instance already stored in the scene; do not clone it into another scene entry.
- [x] Add the rose content to the current scene model without changing the meaning or storage of existing `VaseFlower`, `VaseDaisy`, or `VaseTulipDaisy` data.
- [x] Update `SceneGenerator` to construct the typed rose only after the exact rose prompt has parsed successfully.
- [x] Update `SceneValidator` to enforce one vase, one rose, one stem, one bloom, exactly two leaves, and the closed red/pink color set.
- [x] Reject invalid/null/mismatched rose geometry and mismatched vase references through ordinary validation errors, not routine exceptions.
- [x] Enforce mutual exclusion between rose content and every existing vase/fruit/cup/bowl content relationship.
- [x] Keep the rose out of `SceneEntry`, object-depth sorting, independent shadow generation, and tabletop contact-shadow ownership.
- [x] Ensure the rose's bounds and vase-relative attachment fit the existing vase profile for both blue and cream glazes.
- [x] Do not change the vase profile, vase transform, vessel dimensions, or any unrelated scene object to make room for the rose.
- [x] Preserve the existing maximum scene-entry and composition rules for all non-flower scenes.
- [x] Add Core tests for vase identity, content exclusivity, exact flower counts, bounds, and all validator rejection cases.

### 5. Render rose petals, leaves, and continuous vase contact

- [x] Add a dedicated `RoseMaterial` in the Skia renderer project. Core must remain free of pixel/color material definitions.
- [x] Define separate red and pink petal palettes with coherent base, light, shadow/fold, and edge tones.
- [x] Make the red rose visibly red rather than tulip-red geometry, and make the pink rose visibly pink rather than a washed-out red variant.
- [x] Keep stem and leaf greens consistent with the project's existing illustrated still-life palette while giving the rose petals their own species material.
- [x] Ensure changing vase glaze cannot recolor or otherwise perturb rose material.
- [x] Add rose drawing to the existing vase-first plant pass. Reuse the shared shape-agnostic stem projection and vase-mouth contact helpers where they fit.
- [x] Draw the complete rose stem continuously across the far rim; do not split it into two independently antialiased strokes at the contact point.
- [x] Preserve the tangent-matched below-base continuation and clip it inside the near lip/opening exactly as the existing flowers do.
- [x] Keep the rose head and visible stem above the mouth; no rose petal, leaf, stem, or calyx pixel may leak onto the exterior vase wall.
- [x] Draw petals in deliberate back-to-front layer order so rear petals are occluded by front petals and the central whorl remains legible.
- [x] Add gentle lighting gradients and petal-fold shading that reveal the layered rose structure without making it photorealistic or visually noisy.
- [x] Keep the rose silhouette complete and additive in silhouette mode.
- [x] Keep detail-omission behavior scoped to rose detail; it must not omit the bloom silhouette or corrupt another vase-content path.
- [x] Preserve diagnostic-render isolation: silhouette, shadow-mask, and detail-omission passes must not contaminate subsequent normal renders.
- [x] Preserve the empty-vase shadow mask and tabletop shadows; the rose adds no independent cast/contact shadow in this milestone.
- [x] Preserve the antialiasing option introduced by Phase 9 follow-up PR #53, including its default and UI behavior.
- [x] Ensure the four rose combinations render deterministically to valid PNGs at the existing output dimensions.
- [x] Do not introduce GPU rendering, 3D geometry, external assets, downloaded textures, fonts, models, or neural components.

### 6. Add focused Core, parser, renderer, and regression tests

- [x] Add focused Core tests for rose prompt values, `RoseColor`, `VaseRose`, and `RosePlant` invariants.
- [x] Test all four supported prompts through parse, scene generation, validation, subject-token selection, and safe filename mapping.
- [x] Test unsupported colors/species, plurals, counts, bouquets, reversed wording, alternate containers, extra scene objects, and incomplete prompts.
- [x] Test exact one-bloom/one-stem/two-leaf geometry and the documented fixed petal/layer counts.
- [x] Test petal-layer ordering, overlap, the tight inner whorl, and non-degenerate contours.
- [x] Test both leaves are attached to the sampled stem, separated sufficiently, and included in measured bounds.
- [x] Test geometry determinism for identical inputs and visible changes for selected distinct seeds.
- [x] Test bounded geometry across several seeds and variation values including 0 and 1.
- [x] Test geometry at the project's supported output sizes, including 64 × 64, 1024 × 1024, and wide/tall aspect ratios up to the current 4096 px limit.
- [x] Test rose bounds stay within the canvas and retain the required top margin above the vase.
- [x] Test the stem base is derived from the vase's normalized opening and fits the opening for both vase glazes.
- [x] Test rose geometry remains identical when only the vase glaze changes.
- [x] Test rose random streams do not perturb standalone tulip/daisy or mixed tulip/daisy geometry.
- [x] Test the rose content is mutually exclusive with standalone tulip, standalone daisy, mixed tulip/daisy, fruit containment, and cup contents.
- [x] Add renderer pixel tests proving the rose bloom, central whorl, both leaves, and stem are actually painted.
- [x] Add pixel/contour checks that the rose's layers remain distinguishable and do not collapse into a daisy-like disk or tulip-like cup.
- [x] Test visible stem continuity through the far-rim contact at preview and full-resolution sizes.
- [x] Test the interior stem follows the correct tangent, remains inside the opening, and disappears beneath the near lip.
- [x] Test no rose pixels leak onto the exterior vase wall or appear below the mouth outside the opening.
- [x] Test the rose adds no tabletop shadow, vase-body repainting, external leakage, or unrelated pixel changes.
- [x] Test rose silhouettes include all required plant contours and detail omission removes only allowed details.
- [x] Test red/pink petal material distinction and independence from blue/cream vase glaze.
- [x] Test repeated renders are pixel-identical for the same scene, seed, variation, resolution, and antialias setting.
- [x] Test antialias on/off remains a rasterization choice and does not alter the prompt, geometry, dimensions, or PNG save semantics.
- [x] Add desktop pipeline tests for all four prompts through `GenerationService`, `DesktopSession`, preview metadata, and save metadata.
- [x] Test the four new subject tokens are whitelisted and every earlier save token/filename remains unchanged.
- [x] Test invalid rose prompts retain the last successful preview and its seed/variation metadata.
- [x] Test the prompt-help example and any displayed hint remain consistent with exact supported grammar.
- [x] Run the complete existing test suite; do not limit validation to the new rose tests.

### 7. Desktop and save integration

- [x] Integrate all four rose prompts into the existing desktop generation path without a new screen, panel, editor, or flower-specific control.
- [x] Add one concise rose prompt example to the existing prompt-help area.
- [x] Preserve seed and variation controls, Randomize, Generate, preview retention, duplicate-action guards, Save PNG, and asynchronous save-dialog behavior.
- [x] Preserve the antialias control and its current default/state semantics.
- [x] Add the four rose tokens to `PngSaveService.KnownSubjects` without changing fallback rules or any previous token.
- [x] Confirm filenames remain based on the last successful preview, not subsequently edited prompt/seed/variation controls.
- [x] Confirm save cancellation retains the preview and save failures remain actionable without exposing stack traces.
- [x] Do not claim native Windows, Photino, or WebView2 behavior from Linux headless tests.

### 8. Generate, map, and visually review the rose artifacts

> CI generated and uploaded the preview in review-recording run `42590714896` (artifact `13660659546`, 8,551,852 bytes). The user supplied the two sheets, eight full-size render PNGs, four silhouettes, and all ten `.timing.json` sidecars; visual and timing findings are recorded below. The focused `phase10-rose-review` artifact collects just these rose files in one download while preserving the full `procedural-preview`.

- [x] Generate all four canonical prompt × vase combinations at seed 42 and variation 0.35.
- [x] Generate a 2 × 2 canonical review sheet with 512 × 512 px cells so all four combinations can be compared at a useful size.
- [x] Generate a second 2 × 2 robustness sheet covering at least one alternate seed, low variation, and high variation across both rose colors and both vase glazes.
- [x] Generate and retain the corresponding individual 1024 × 1024 PNGs for the four canonical cases and the selected robustness cases.
- [x] Generate silhouettes for the canonical rose cases using the existing artifact conventions.
- [x] Generate timing/geometry sidecars for every retained sample and a sheet sidecar that maps every cell to prompt, color, glaze, seed, variation, filename, and dimensions.
- [x] Keep generated files under the existing test-artifact output path so the established `procedural-preview` upload collects them.
- [x] Do not commit generated PNGs, timing dumps, or temporary diagnostic files as source.
- [x] Inspect all four canonical sheet cells at 512 px and their corresponding individual 1024 px renders.
- [x] Inspect every robustness-sheet cell for the same shape and contact properties; do not treat successful PNG generation as visual review.
- [x] Confirm the rose reads as a rose immediately, with a layered cupped head and a clear inner whorl.
- [x] Confirm the outer, middle, and inner petal layers remain visible and do not merge into a flat red/pink mass.
- [x] Confirm red and pink variants are clearly distinct at both vase glazes.
- [x] Confirm the rose is neither a recoloured tulip nor a radial daisy.
- [x] Confirm both leaves remain visible and attached, and the bloom, calyx/sepals if present, stem, and vase mouth do not collide awkwardly.
- [x] Confirm the stem enters the vase continuously and disappears naturally behind the near lip at both 512 px review size and 1024 px full resolution.
- [x] Confirm the vase rim, mouth, body, glaze, foot, tabletop, lighting, and contact shadow have no regressions.
- [x] Review visual balance at normal desktop preview size; if a core rose feature disappears at that size, treat it as a defect rather than hiding it with an enlarged crop.
- [x] Read the timing sidecars and record only environment-specific observations. Do not claim a general performance improvement or per-flower cost from a single Stopwatch sample.
- [x] Record cosmetic observations separately from defects; do not expand the milestone to unrelated visual preferences.
### 9. Documentation, verification, and handoff

> Agent-side visual review and the sidecar timing observations are recorded in the handoff and README. CI now includes a focused one-download `phase10-rose-review` artifact in response to the user's feedback. The PR is intentionally unmerged, so no post-merge run or merge commit exists.

- [x] Update the README current-capability section with standalone red/pink roses in blue/cream vases.
- [x] Update the prompt grammar documentation with the exact four rose prompts and explicit exclusions.
- [x] Update the architecture overview with the new species-specific rose content path; keep existing tulip/daisy models species-specific.
- [x] Document the rose geometry decisions, fixed counts/layers, normalized vase-relative placement, named random streams, and material separation.
- [x] Document the shared renderer draw order and continuous tangent-matched vase stem contact; do not duplicate outdated pre-PR-53 behavior.
- [x] Document the four rose subject tokens and default filename forms.
- [x] Add `docs/rose-vase-handoff.md` with starting/ending commits, Phase 9 prerequisite evidence, model and geometry decisions, prompt grammar, rendering/occlusion, tests, artifacts, visual-review status, timing observations, and known limitations.
- [x] Keep `.agents/todo-10.md` updated throughout implementation.
- [x] Mark a checkbox `[x]` only after the work and its stated verification are complete.
- [x] Leave blocked, unexecuted, or human-review tasks unchecked and explain the exact evidence or blocker beside them.
- [x] Run restore, Release warnings-as-errors build, and the full solution test workflow through the existing CI gate.
- [x] Confirm the PR source/test CI run passes the full solution build and test steps, and that the `procedural-preview` artifact was uploaded.
- [ ] Confirm the merged build's post-merge Linux Release CI only after the PR is merged; do not describe a PR run as post-merge.
- [x] Keep the existing single Linux Release workflow. Do not add a permanent Debug job, Windows runner, package, project, workflow job, or CI service for this milestone.
- [x] Add a focused `phase10-rose-review` artifact to the existing job, so the rose review images and sidecars can be downloaded as one curated bundle alongside the comprehensive preview.
- [x] Record the available evidence: PR #55, CI run `42590714896`, preview artifact `13660659546`, and this handoff. There is no merge commit because the PR remains open by instruction.
- [x] Reconcile the checklist, README, handoff, tests, and changed-file list; identify anything authored but not executed.
- [x] Do not claim native Photino/WebView2 dialogs, window lifecycle, resize, or accessibility behavior was tested from headless CI.
- [x] Create focused PR #55 and leave it open for review; do not merge without explicit user approval.
- [x] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, artifact location, visual observations, timing limitations, and known exclusions.

## Acceptance checklist

- [x] The four exact red/pink rose × blue/cream vase prompts parse and generate through the existing desktop prompt-to-PNG path.
- [x] A rose is represented as one typed vase content referring to the exact vase instance; the vase remains the only scene entry.
- [x] The rose contains exactly one bloom, one continuous stem, and two attached leaves, with the documented fixed layered-petal structure.
- [x] The rose is recognisably distinct from both the tulip and daisy at normal viewing size.
- [x] Red and pink material variants remain clearly distinguishable and independent of vase glaze.
- [x] Rose geometry is normalized, deterministic, bounded, vase-profile-aware, and stable across supported sizes and variation values.
- [x] The shared stem path crosses the far rim continuously, follows the correct interior tangent, and disappears behind the near lip without wall leakage.
- [x] Existing fruit, ceramic, containment, beverage, standalone tulip, standalone daisy, and mixed tulip/daisy prompts and save paths remain supported.
- [x] Automated Core/parser, geometry, renderer pixel, desktop pipeline, save-token, and full regression tests pass.
- [x] The canonical and robustness artifacts have been visually reviewed; README and handoff accurately document evidence and limitations.
- [x] Linux Release CI passes on the Phase 10 PR head, the preview artifact is retained, and the agent stops before merge.

## Explicitly out of scope

- Adding a rose to the Phase 9 tulip/daisy pair, a three-flower arrangement, or any bouquet.
- Multiple roses, buds, dropped petals, multiple stems, arbitrary quantities, or user-controlled placement.
- Any flower species beyond the existing tulip/daisy and this standalone rose.
- Any rose color beyond red and pink; any vase glaze beyond blue and cream.
- Flowers in bowls/cups, liquids in vases, fruit in vases, multiple vases, or new contents relationships.
- New vase profiles, new ceramic categories, tabletop/background redesign, or unrelated material changes.
- General-purpose plant families, arbitrary flower collections, a bouquet editor, scene graph, object registry, plugin architecture, or unrestricted natural-language parser.
- Animation, growth, wind, botanical simulation, physics, 3D, GPU rendering, neural models, LLM prompt translation, downloaded artwork, stock textures, fonts, or external assets.
- New desktop panels, camera controls, a visual editor, new packages, projects, frameworks, CI runners, or permanent workflow jobs.
