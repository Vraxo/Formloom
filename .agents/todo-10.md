# Phase 10 — A Red or Pink Rose in a Ceramic Vase

> **Status:** Ready for implementation. This is a narrow, standalone third-flower milestone built on the existing vase-content and renderer paths. Start from the latest `main`; do not assume the current commit is still the one recorded below.

## Goal

Add one recognisable red or pink rose growing from one existing blue or cream ceramic vase. The rose is a new species with its own typed Core model, normalized geometry and Skia material. Its bloom must read as a layered rose, not as a recoloured tulip or a daisy with a different centre.

The four supported prompts are:

1. a red rose in a blue ceramic vase on a wooden table
2. a red rose in a cream ceramic vase on a wooden table
3. a pink rose in a blue ceramic vase on a wooden table
4. a pink rose in a cream ceramic vase on a wooden table

Exactly one rose, one stem, and two leaves. The existing vase remains the only scene entry. This phase adds a standalone rose prompt only; it does not add roses to the existing tulip/daisy pair.

## Phase 9 prerequisite — verified evidence and required documentation reconciliation

Phase 9's follow-up was merged after its checklist and handoff were written. The latest verified main commit at the time this specification was authored is `02286c55c21ed46cc332796c2861b3354ca6dd20`.

- Phase 9 implementation PR [#52](https://github.com/Vraxo/Formloom/pull/52) merged as `1311f91e582554c2601c0b9476265a1a62ab123d`; its post-merge Linux Release run [42493951766](https://github.com/Vraxo/Formloom/actions/runs/42493951766) passed.
- Vase stem-contact follow-up PR [#53](https://github.com/Vraxo/Formloom/pull/53) merged as `02286c55c21ed46cc332796c2861b3354ca6dd20`; its post-merge Linux Release run [42507895872](https://github.com/Vraxo/Formloom/actions/runs/42507895872) passed.
- The PR #53 workflow artifact is artifact ID `13628551050` (`procedural-preview`). It contains the 1280 × 960 Phase 9 review sheet, ten 1024 × 1024 mixed-arrangement samples, their individual timing sidecars, and the sheet timing/manifest sidecar.
- The sheet is 4 columns × 3 rows with 320 × 320 cells. Its first row is the four red/yellow × blue/cream canonical combinations; the remaining six cells show alternate seeds and variation extremes.
- Artifact review performed on 2026-11-09: all ten sheet cells were inspected; the four canonical seed-42 / variation-0.35 combinations were also inspected at 1024 × 1024, along with representative high-variation samples. The two blooms remain distinguishable, all four leaves are visible, both stems enter the vase continuously, and the near lip hides the interior continuations. No blocking visual defect was found in that review. This records assistant-side artifact inspection, not a claim of separate user acceptance.
- All ten 1024 × 1024 timing sidecars were read. Their observed render-and-encode values range from approximately 132.8 ms to 192.4 ms; the four canonical cases range from approximately 132.8 ms to 137.6 ms. The 320 × 320 sheet cells range from approximately 31.4 ms to 45.6 ms. These are observations from .NET 10.0.14 on Ubuntu 24.04.5 LTS with four reported processors, not performance guarantees or a universal cost model.

The current Phase 9 checklist still has unchecked review items and says PR #53 is unmerged. The Phase 9 handoff also contains stale statements that the other samples were not reviewed, that timings were not read, and that PR #53 must not be merged. Those statements are obsolete in light of the merged PR, successful post-merge run, and artifact review above.

## Sequential implementation checklist

### 0. Verify the starting point and close the Phase 9 record

- [x] Confirm that Phase 9 PR #52 merged as `1311f91e582554c2601c0b9476265a1a62ab123d` and its post-merge Linux Release run `42493951766` succeeded.
- [x] Confirm that Phase 9 follow-up PR #53 merged as `02286c55c21ed46cc332796c2861b3354ca6dd20` and post-merge run `42507895872` succeeded.
- [x] Confirm that artifact `13628551050` contains the Phase 9 4 × 3 sheet, ten full-resolution mixed-arrangement images, ten individual timing sidecars, and the sheet timing sidecar.
- [x] Inspect the ten 320 px review-sheet cells and the four canonical 1024 px samples; inspect representative high-variation samples. No blocking visual issue was found.
- [x] Read the ten full-resolution timing sidecars and the sheet timing sidecar; record their values and environment-specific limitations above.
- [ ] Start from the latest `main`, record its exact HEAD, and confirm that the working tree/branch has no unrelated changes. The recorded `02286c5` is the verified prerequisite commit, not an instruction to reset or force the branch backwards.
- [ ] Re-read `.agents/todo-9.md` and `docs/mixed-vase-arrangement-handoff.md` from the current `main`; verify their contents against the merged source, CI run `42507895872`, and artifact `13628551050`.
- [ ] Reconcile the stale PR #53 unmerged/pending language in both Phase 9 documents. Replace it with the actual merge commit and successful post-merge run.
- [ ] Reconcile Phase 9's remaining review/timing checkboxes with the exact inspection scope and artifact evidence recorded above. Mark only what is supported; preserve any genuinely unverified limitation.
- [ ] Record the Phase 9 visual review as assistant-side artifact inspection, not user acceptance. Distinguish the ten 320 px sheet-cell checks from the four canonical and representative individual full-resolution checks.
- [ ] Preserve the measured timing range as environment-specific observations. Do not claim that the new renderer is faster/slower from a single noisy measurement or compare incomparable sample sizes.
- [ ] If the current repository has advanced beyond the recorded commit, inspect all intervening changes and carry the phase forward from the actual latest `main`.
- [ ] If Phase 9 source, visual, or CI evidence materially contradicts this verified record, stop and report the exact discrepancy before implementing Phase 10. Do not silently bury a Phase 9 defect in the rose work.
- [ ] Confirm `.agents/todo-10.md` is the only new milestone specification being added by this phase; do not overwrite earlier checklists or handoffs.

**Phase 9 prerequisite gate:** continue only after the two Phase 9 documents accurately describe the merged and reviewed state, and the current `main` has passed its latest required Release CI. A newly discovered blocking issue must be resolved or reported before rose implementation begins.

### 1. Inspect the current architecture and define the narrow rose model

- [ ] Read the current Core, renderer, parser, scene validation, desktop generation/save, and test implementations before editing.
- [ ] Trace the four existing vase-content paths: standalone tulip, standalone daisy, the fixed tulip/daisy pair, and an empty vase.
- [ ] Trace the complete prompt → Core → scene generation → validation → Skia render → DesktopSession → PNG save path for an existing standalone flower.
- [ ] Inspect the current `FlowerKind`, `VaseMouth`, `VaseStem` projection/helpers, and shared vase-first plant draw order in the current source. Names and paths in this document are orientation only.
- [ ] Confirm how red/yellow tulip and white/golden daisy appearances are represented and validated. Do not widen those models to accommodate rose-specific data.
- [ ] Confirm how the renderer carries a complete stem through the far rim and clips its tangent-matched interior continuation behind the near lip.
- [ ] Confirm how flower geometry, vase profile, transforms, bounds, variation, and named random streams are represented.
- [ ] Choose a species-specific `RosePlant` model and its small set of supporting geometry records. Keep rose petal geometry separate from `TulipPetal` and `DaisyPetal`.
- [ ] Add a closed `RoseColor` representation with exactly `Red` and `Pink`, or an equally narrow closed appearance representation if the existing conventions make that more coherent.
- [ ] Add a typed `VaseRose` vase-content record that refers to the exact vase instance already held by the scene.
- [ ] Add `FlowerKind.Rose` and update all exhaustive validation/dispatch sites deliberately. Do not make `FlowerKind` a stringly typed extension point.
- [ ] Keep the vase as the only `SceneEntry`; the rose is vase content, not an independently transformed scene object with its own depth or shadow.
- [ ] Make rose contents mutually exclusive with tulip-only, daisy-only, tulip/daisy pair, bowl containment, and cup contents.
- [ ] Keep Core independent of SkiaSharp, pixel coordinates, bitmap/native resources, and Desktop/UI types.
- [ ] Avoid broad nullable flower records, arbitrary flower arrays, a general plant registry, a generic bouquet model, reflection dispatch, or a universal contents abstraction.
- [ ] Record the chosen typed model and exact invariants in the Phase 10 handoff.

### 2. Add exact rose prompt grammar and save identity

- [ ] Add a rose prompt value type following the current prompt-model conventions.
- [ ] Support exactly the four prompts listed at the top of this document: red/pink rose × blue/cream vase.
- [ ] Use exact grammar: `a red rose` or `a pink rose`, followed by `in`, exactly one existing blue/cream ceramic vase descriptor, and `on a wooden table`.
- [ ] Preserve the current case normalization, repeated-whitespace normalization, and terminal-punctuation handling.
- [ ] Preserve the existing strict parsing contract. Do not introduce synonyms, flexible word order, plural inference, adjective interpretation, or natural-language fallbacks.
- [ ] Ensure rose parsing is attempted before any broader parser branch that could incorrectly accept or degrade a malformed rose prompt.
- [ ] Reject an unspecified rose, white/yellow/blue/purple rose, alternate flower color, alternate vase glaze, and every unsupported species.
- [ ] Reject roses in bowls/cups, roses beside a vase, vase-first phrasing, reversed relationships, missing table phrases, extra scene objects, and malformed separators.
- [ ] Reject plural roses, two roses, bouquets, rose/tulip or rose/daisy mixtures, and any third flower.
- [ ] Verify that each invalid prompt is rejected as a whole and never falls back to an empty vase, a tulip, or a daisy.
- [ ] Add parser tests for all four supported prompts and each established normalization behavior.
- [ ] Add a focused negative-prompt matrix for unsupported color, species, order, quantity, container, relationship, and extra-object variants.
- [ ] Preserve every pre-existing prompt, parser result shape, rejection behavior, and subject token.

Expected subject tokens and default seed-42 save names:

| Prompt | Subject token | Default filename |
| --- | --- | --- |
| red rose / blue vase | `red-rose-in-blue-vase` | `formloom-red-rose-in-blue-vase-seed-42.png` |
| red rose / cream vase | `red-rose-in-cream-vase` | `formloom-red-rose-in-cream-vase-seed-42.png` |
| pink rose / blue vase | `pink-rose-in-blue-vase` | `formloom-pink-rose-in-blue-vase-seed-42.png` |
| pink rose / cream vase | `pink-rose-in-cream-vase` | `formloom-pink-rose-in-cream-vase-seed-42.png` |

- [ ] Add exactly these four new subject tokens to the existing safe-save whitelist.
- [ ] Verify seed, variation, extension, filename normalization, and whitelist fallback behavior.
- [ ] Verify that rejected prompts retain the last successful preview and its associated metadata.

### 3. Construct a recognisable rose from normalized geometry

- [ ] Add a dedicated `RoseGeometry` generator and species-specific records such as `RoseBloom`, `RosePetal`, `RoseLeaf`, and `RosePlant`; use separate files for primary types in keeping with repository conventions.
- [ ] Make every rose contour and attachment coordinate normalized and derived from the existing vase profile/body, never from fixed 1024 px placement constants.
- [ ] Derive the stem base from the same `VaseMouth` opening helpers used by the existing flower species.
- [ ] Derive the bloom size, height, and top margin from the vase opening and available headroom; do not let the rose collide with the vase rim or canvas edge.
- [ ] Give the rose a compact, layered, cupped silhouette with overlapping outer, middle, and inner petal layers.
- [ ] Shape the central whorl as a visibly tighter, partially curled arrangement; do not add a daisy-like centre disk.
- [ ] Make petal overlap and depth order deliberate so roots are hidden where appropriate and the inner whorl remains readable.
- [ ] Use visibly curved petal edges and bounded asymmetry; avoid a stack of identical circles, flat radial spokes, or a single scalloped blob.
- [ ] Keep the outer petal silhouette broad enough to read as a rose while preserving enough negative space and contour separation to distinguish individual layers.
- [ ] Ensure the bloom is visibly different from the tulip's five-part cupped shape and the daisy's twelve-petal radial head.
- [ ] Choose and document a small fixed petal/layer count suitable for the rendered size. Keep counts stable; variation may reshape petals but must not randomly add/remove them.
- [ ] Derive petal widths, lengths, curvature, overlap, and layer radii from normalized rose dimensions rather than canvas pixels.
- [ ] Add restrained directional shading and fold contours in the geometry/material boundary without embedding RGB values in Core.
- [ ] Add a small green calyx/sepals only if they materially improve the bloom-to-stem transition; keep them attached to the rose head and avoid expanding the object count or scope.
- [ ] Create exactly one continuous stem with a bounded natural curve and a thickness proportionate to the vase opening and bloom.
- [ ] Create exactly two leaf blades attached to the stem at separate heights. Keep their attachment points and direction deterministic and their silhouettes visible at normal viewing size.
- [ ] Give rose leaves a restrained species-appropriate shape, including a clear central vein and optional subtle serration if it improves recognition without creating noisy edges.
- [ ] Ensure leaves do not merge into the bloom, each other, or the vase rim; both must remain visibly attached to the stem.
- [ ] Measure `RosePlant.Bounds` from the actual bloom, stem, calyx/sepals if present, and both leaf contours. Do not accept caller-supplied bounds that can disagree with drawn geometry.
- [ ] Use stable, independently named random streams for rose stem, leaves, and petal/layer detail. Do not use `GetHashCode()` or mutable shared PRNG order to derive seed identity.
- [ ] Keep variation bounded and continuous from 0 to 1; it may alter contour, rotation, curvature, and offsets but must not break the rose's identity or fixed geometry counts.
- [ ] Prove identical prompt/seed/variation inputs produce identical Core geometry and that different seeds create visible, coherent changes.
- [ ] Prove rose randomness does not perturb existing tulip, daisy, or tulip/daisy-pair geometry.
- [ ] Prove changing only the vase glaze does not change the rose's geometry.
- [ ] Test normalized bounds and vase-relative clearances across representative seeds, variation extremes, and all supported canvas aspect ratios.
- [ ] Keep the rose composition centred and visually balanced above the vase; do not introduce a user-controlled layout or arbitrary flower-placement mechanism.

### 4. Integrate the rose with the existing vase-content path

- [ ] Add `VaseRose.Create` or the equivalent focused factory using the existing vase profile and normalized geometry conventions.
- [ ] Preserve the exact vase instance already stored in the scene; do not clone it into another scene entry.
- [ ] Add the rose content to the current scene model without changing the meaning or storage of existing `VaseFlower`, `VaseDaisy`, or `VaseTulipDaisy` data.
- [ ] Update `SceneGenerator` to construct the typed rose only after the exact rose prompt has parsed successfully.
- [ ] Update `SceneValidator` to enforce one vase, one rose, one stem, one bloom, exactly two leaves, and the closed red/pink color set.
- [ ] Reject invalid/null/mismatched rose geometry and mismatched vase references through ordinary validation errors, not routine exceptions.
- [ ] Enforce mutual exclusion between rose content and every existing vase/fruit/cup/bowl content relationship.
- [ ] Keep the rose out of `SceneEntry`, object-depth sorting, independent shadow generation, and tabletop contact-shadow ownership.
- [ ] Ensure the rose's bounds and vase-relative attachment fit the existing vase profile for both blue and cream glazes.
- [ ] Do not change the vase profile, vase transform, vessel dimensions, or any unrelated scene object to make room for the rose.
- [ ] Preserve the existing maximum scene-entry and composition rules for all non-flower scenes.
- [ ] Add Core tests for vase identity, content exclusivity, exact flower counts, bounds, and all validator rejection cases.

### 5. Render rose petals, leaves, and continuous vase contact

- [ ] Add a dedicated `RoseMaterial` in the Skia renderer project. Core must remain free of pixel/color material definitions.
- [ ] Define separate red and pink petal palettes with coherent base, light, shadow/fold, and edge tones.
- [ ] Make the red rose visibly red rather than tulip-red geometry, and make the pink rose visibly pink rather than a washed-out red variant.
- [ ] Keep stem and leaf greens consistent with the project's existing illustrated still-life palette while giving the rose petals their own species material.
- [ ] Ensure changing vase glaze cannot recolor or otherwise perturb rose material.
- [ ] Add rose drawing to the existing vase-first plant pass. Reuse the shared shape-agnostic stem projection and vase-mouth contact helpers where they fit.
- [ ] Draw the complete rose stem continuously across the far rim; do not split it into two independently antialiased strokes at the contact point.
- [ ] Preserve the tangent-matched below-base continuation and clip it inside the near lip/opening exactly as the existing flowers do.
- [ ] Keep the rose head and visible stem above the mouth; no rose petal, leaf, stem, or calyx pixel may leak onto the exterior vase wall.
- [ ] Draw petals in deliberate back-to-front layer order so rear petals are occluded by front petals and the central whorl remains legible.
- [ ] Add gentle lighting gradients and petal-fold shading that reveal the layered rose structure without making it photorealistic or visually noisy.
- [ ] Keep the rose silhouette complete and additive in silhouette mode.
- [ ] Keep detail-omission behavior scoped to rose detail; it must not omit the bloom silhouette or corrupt another vase-content path.
- [ ] Preserve diagnostic-render isolation: silhouette, shadow-mask, and detail-omission passes must not contaminate subsequent normal renders.
- [ ] Preserve the empty-vase shadow mask and tabletop shadows; the rose adds no independent cast/contact shadow in this milestone.
- [ ] Preserve the antialiasing option introduced by Phase 9 follow-up PR #53, including its default and UI behavior.
- [ ] Ensure the four rose combinations render deterministically to valid PNGs at the existing output dimensions.
- [ ] Do not introduce GPU rendering, 3D geometry, external assets, downloaded textures, fonts, models, or neural components.

### 6. Add focused Core, parser, renderer, and regression tests

- [ ] Add focused Core tests for rose prompt values, `RoseColor`, `VaseRose`, and `RosePlant` invariants.
- [ ] Test all four supported prompts through parse, scene generation, validation, subject-token selection, and safe filename mapping.
- [ ] Test unsupported colors/species, plurals, counts, bouquets, reversed wording, alternate containers, extra scene objects, and incomplete prompts.
- [ ] Test exact one-bloom/one-stem/two-leaf geometry and the documented fixed petal/layer counts.
- [ ] Test petal-layer ordering, overlap, the tight inner whorl, and non-degenerate contours.
- [ ] Test both leaves are attached to the sampled stem, separated sufficiently, and included in measured bounds.
- [ ] Test geometry determinism for identical inputs and visible changes for selected distinct seeds.
- [ ] Test bounded geometry across several seeds and variation values including 0 and 1.
- [ ] Test geometry at the project's supported output sizes, including 64 × 64, 1024 × 1024, and wide/tall aspect ratios up to the current 4096 px limit.
- [ ] Test rose bounds stay within the canvas and retain the required top margin above the vase.
- [ ] Test the stem base is derived from the vase's normalized opening and fits the opening for both vase glazes.
- [ ] Test rose geometry remains identical when only the vase glaze changes.
- [ ] Test rose random streams do not perturb standalone tulip/daisy or mixed tulip/daisy geometry.
- [ ] Test the rose content is mutually exclusive with standalone tulip, standalone daisy, mixed tulip/daisy, fruit containment, and cup contents.
- [ ] Add renderer pixel tests proving the rose bloom, central whorl, both leaves, and stem are actually painted.
- [ ] Add pixel/contour checks that the rose's layers remain distinguishable and do not collapse into a daisy-like disk or tulip-like cup.
- [ ] Test visible stem continuity through the far-rim contact at preview and full-resolution sizes.
- [ ] Test the interior stem follows the correct tangent, remains inside the opening, and disappears beneath the near lip.
- [ ] Test no rose pixels leak onto the exterior vase wall or appear below the mouth outside the opening.
- [ ] Test the rose adds no tabletop shadow, vase-body repainting, external leakage, or unrelated pixel changes.
- [ ] Test rose silhouettes include all required plant contours and detail omission removes only allowed details.
- [ ] Test red/pink petal material distinction and independence from blue/cream vase glaze.
- [ ] Test repeated renders are pixel-identical for the same scene, seed, variation, resolution, and antialias setting.
- [ ] Test antialias on/off remains a rasterization choice and does not alter the prompt, geometry, dimensions, or PNG save semantics.
- [ ] Add desktop pipeline tests for all four prompts through `GenerationService`, `DesktopSession`, preview metadata, and save metadata.
- [ ] Test the four new subject tokens are whitelisted and every earlier save token/filename remains unchanged.
- [ ] Test invalid rose prompts retain the last successful preview and its seed/variation metadata.
- [ ] Test the prompt-help example and any displayed hint remain consistent with exact supported grammar.
- [ ] Run the complete existing test suite; do not limit validation to the new rose tests.

### 7. Desktop and save integration

- [ ] Integrate all four rose prompts into the existing desktop generation path without a new screen, panel, editor, or flower-specific control.
- [ ] Add one concise rose prompt example to the existing prompt-help area.
- [ ] Preserve seed and variation controls, Randomize, Generate, preview retention, duplicate-action guards, Save PNG, and asynchronous save-dialog behavior.
- [ ] Preserve the antialias control and its current default/state semantics.
- [ ] Add the four rose tokens to `PngSaveService.KnownSubjects` without changing fallback rules or any previous token.
- [ ] Confirm filenames remain based on the last successful preview, not subsequently edited prompt/seed/variation controls.
- [ ] Confirm save cancellation retains the preview and save failures remain actionable without exposing stack traces.
- [ ] Do not claim native Windows, Photino, or WebView2 behavior from Linux headless tests.

### 8. Generate, map, and visually review the rose artifacts

- [ ] Generate all four canonical prompt × vase combinations at seed 42 and variation 0.35.
- [ ] Generate a 2 × 2 canonical review sheet with 512 × 512 px cells so all four combinations can be compared at a useful size.
- [ ] Generate a second 2 × 2 robustness sheet covering at least one alternate seed, low variation, and high variation across both rose colors and both vase glazes.
- [ ] Generate and retain the corresponding individual 1024 × 1024 PNGs for the four canonical cases and the selected robustness cases.
- [ ] Generate silhouettes for the canonical rose cases using the existing artifact conventions.
- [ ] Generate timing/geometry sidecars for every retained sample and a sheet sidecar that maps every cell to prompt, color, glaze, seed, variation, filename, and dimensions.
- [ ] Keep generated files under the existing test-artifact output path so the established `procedural-preview` upload collects them.
- [ ] Do not commit generated PNGs, timing dumps, or temporary diagnostic files as source.
- [ ] Inspect all four canonical sheet cells at 512 px and their corresponding individual 1024 px renders.
- [ ] Inspect every robustness-sheet cell for the same shape and contact properties; do not treat successful PNG generation as visual review.
- [ ] Confirm the rose reads as a rose immediately, with a layered cupped head and a clear inner whorl.
- [ ] Confirm the outer, middle, and inner petal layers remain visible and do not merge into a flat red/pink mass.
- [ ] Confirm red and pink variants are clearly distinct at both vase glazes.
- [ ] Confirm the rose is neither a recoloured tulip nor a radial daisy.
- [ ] Confirm both leaves remain visible and attached, and the bloom, calyx/sepals if present, stem, and vase mouth do not collide awkwardly.
- [ ] Confirm the stem enters the vase continuously and disappears naturally behind the near lip at both 512 px review size and 1024 px full resolution.
- [ ] Confirm the vase rim, mouth, body, glaze, foot, tabletop, lighting, and contact shadow have no regressions.
- [ ] Review visual balance at normal desktop preview size; if a core rose feature disappears at that size, treat it as a defect rather than hiding it with an enlarged crop.
- [ ] Read the timing sidecars and record only environment-specific observations. Do not claim a general performance improvement or per-flower cost from a single Stopwatch sample.
- [ ] Record cosmetic observations separately from defects; do not expand the milestone to unrelated visual preferences.

### 9. Documentation, verification, and handoff

- [ ] Update the README current-capability section with standalone red/pink roses in blue/cream vases.
- [ ] Update the prompt grammar documentation with the exact four rose prompts and explicit exclusions.
- [ ] Update the architecture overview with the new species-specific rose content path; keep existing tulip/daisy models species-specific.
- [ ] Document the rose geometry decisions, fixed counts/layers, normalized vase-relative placement, named random streams, and material separation.
- [ ] Document the shared renderer draw order and continuous tangent-matched vase stem contact; do not duplicate outdated pre-PR-53 behavior.
- [ ] Document the four rose subject tokens and default filename forms.
- [ ] Add `docs/rose-vase-handoff.md` with starting/ending commits, Phase 9 prerequisite evidence, model and geometry decisions, prompt grammar, rendering/occlusion, tests, artifacts, visual-review status, timing observations, and known limitations.
- [ ] Keep `.agents/todo-10.md` updated throughout implementation.
- [ ] Mark a checkbox `[x]` only after the work and its stated verification are complete.
- [ ] Leave blocked, unexecuted, or human-review tasks unchecked and explain the exact evidence or blocker beside them.
- [ ] Run restore, Release warnings-as-errors build, and the full solution test workflow through the existing CI gate.
- [ ] Confirm the final PR CI run passes the full solution build and test steps, and that the `procedural-preview` artifact was uploaded.
- [ ] Confirm the merged build's post-merge Linux Release CI only after the PR is merged; do not describe a PR run as post-merge.
- [ ] Keep the existing single Linux Release workflow. Do not add a permanent Debug job, Windows runner, package, project, workflow job, or CI service for this milestone.
- [ ] Record the exact PR number, merge commit, CI run, artifact ID, and final handoff evidence when available.
- [ ] Reconcile the checklist, README, handoff, tests, and changed-file list; identify anything authored but not executed.
- [ ] Do not claim native Photino/WebView2 dialogs, window lifecycle, resize, or accessibility behavior was tested from headless CI.
- [ ] Create a focused pull request for Phase 10 and leave it open for review; do not merge without explicit user approval.
- [ ] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, artifact location, visual observations, timing limitations, and known exclusions.

## Acceptance checklist

- [ ] The four exact red/pink rose × blue/cream vase prompts parse and generate through the existing desktop prompt-to-PNG path.
- [ ] A rose is represented as one typed vase content referring to the exact vase instance; the vase remains the only scene entry.
- [ ] The rose contains exactly one bloom, one continuous stem, and two attached leaves, with the documented fixed layered-petal structure.
- [ ] The rose is recognisably distinct from both the tulip and daisy at normal viewing size.
- [ ] Red and pink material variants remain clearly distinguishable and independent of vase glaze.
- [ ] Rose geometry is normalized, deterministic, bounded, vase-profile-aware, and stable across supported sizes and variation values.
- [ ] The shared stem path crosses the far rim continuously, follows the correct interior tangent, and disappears behind the near lip without wall leakage.
- [ ] Existing fruit, ceramic, containment, beverage, standalone tulip, standalone daisy, and mixed tulip/daisy prompts and save paths remain supported.
- [ ] Automated Core/parser, geometry, renderer pixel, desktop pipeline, save-token, and full regression tests pass.
- [ ] The canonical and robustness artifacts have been visually reviewed; README and handoff accurately document evidence and limitations.
- [ ] Linux Release CI passes on the Phase 10 PR head, the preview artifact is retained, and the agent stops before merge.

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
