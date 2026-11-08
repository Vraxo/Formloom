# Formloom — Phase 9: A Tulip and Daisy Together in a Ceramic Vase

## Objective

Extend the existing vase-flower capability from one flower to one deliberately composed **two-flower arrangement: exactly one red or yellow tulip and exactly one white daisy with a golden-yellow centre, sharing one existing blue or cream ceramic vase**.

This milestone is about arrangement and interaction between the two already-supported species, not about adding another flower. It must use the existing tulip and daisy profiles rather than redesigning either flower, and it must make both blooms, both stems, and all four leaves legible in one coherent still life.

The arrangement consists of exactly:
- One existing red or yellow tulip bloom.
- One existing white daisy bloom with its golden-yellow central disk.
- One green stem for each bloom.
- Exactly two green leaves attached to each stem (four leaves total).
- One existing blue or cream ceramic vase.

Both stems must enter the same vase opening. Their visible paths must be distinct and physically plausible; the two blooms must not collide, merge into an unreadable mass, or look like separate flowers pasted over the background.

The existing single-tulip and single-daisy scenes must remain unchanged. Do not add a new flower species, vase type, scene object, arbitrary bouquet editor, or general botanical framework.

## Starting point

- Repository: `https://github.com/Vraxo/Formloom`, branch `main`.
- At authoring time, latest `main` is `c441a9a8b2537a577c1bb577c74db0112873393b` (Phase 8 close-out merge, PR #51).
- At authoring time, Phase 8 post-merge Linux Release CI run `42452733510` is green on that commit.
- The values above are an authoring-time reference only. Start from the latest `main`, record the actual HEAD commit, and confirm a clean working tree before editing.
- Previous milestone checklist: `.agents/todo-8.md`.
- Previous milestone handoff: `docs/daisy-vase-handoff.md`.
- Tulip handoff: `docs/vase-flower-handoff.md`.
- Source projects live under `src/`; tests live under `tests/`.
- Stack: .NET 10 / C# 14, PhotinoX.Blazor desktop, SkiaSharp CPU rendering, xUnit v3, existing Linux Release CI.
- Existing flower content is represented by two deliberately species-specific typed vase contents: `VaseFlower` for tulips and `VaseDaisy` for daisies. They share only shape-agnostic vase-mouth and stem-rendering helpers.
- The existing renderer uses normalized geometry, vase-profile-aware stem attachment, bounded named random streams, explicit flower clipping, and a dedicated continuation pass for stems inside the vase opening.
- Inspect the actual current code before choosing exact types, files, or draw order. Names and paths in this document are orientation, not permission to assume implementation details have stayed fixed.

## Phase 8 prerequisite

- Phase 8 must be complete before Phase 9 implementation begins.
- Verify `.agents/todo-8.md`, `docs/daisy-vase-handoff.md`, the merged Phase 8 PR, and its post-merge Linux Release CI run.
- Confirm that the handoff's visual-review and timing statements are supported by the final merged-build evidence.
- If Phase 8 is not actually closed on the latest main, do not build Phase 9 on top of an unverified state. Report the exact missing evidence and stop at the prerequisite checkpoint; do not silently mark Phase 8 complete.
- Do not repeat Phase 8 implementation or reopen accepted Phase 8 design decisions unless a concrete regression caused by this milestone requires a narrowly scoped fix.

## Exact scope and prompt vocabulary

### Preserve all existing vocabulary

Every existing supported prompt and behavior must continue working:
- All existing fruit descriptors, fruit-only groups, and mixed fruit/ceramic scenes.
- Blue and cream ceramic bowls, cups, and vases.
- Fruit-in-bowl containment.
- Coffee/tea-in-cup contents.
- Red/yellow tulip alone in a blue/cream vase.
- White daisy alone in a blue/cream vase.
- Existing seed, variation, normalization, rejection, preview-retention, subject-token, and safe-save-filename behavior.

### New mixed-arrangement prompts

Support exactly these four prompts:

1. `a red tulip and a white daisy in a blue ceramic vase on a wooden table`
2. `a red tulip and a white daisy in a cream ceramic vase on a wooden table`
3. `a yellow tulip and a white daisy in a blue ceramic vase on a wooden table`
4. `a yellow tulip and a white daisy in a cream ceramic vase on a wooden table`

Continue the existing case normalization, repeated-whitespace normalization, and terminal-punctuation behavior. Preserve exact-grammar parsing; do not add synonyms, flexible word order, or free-form natural-language understanding.

The supported order is intentionally fixed: tulip first, then daisy, followed by the shared vase phrase. Do not silently accept reversed or alternative formulations unless the current parser's established normalization explicitly makes them equivalent.

### Explicitly unsupported

- Two tulips, two daisies, or any same-species pair.
- More than two blooms, more than two stems, more than four leaves, buds, dropped petals, or a third flower.
- Any flower species other than the existing tulip and daisy.
- Any tulip colour other than red or yellow.
- Any daisy appearance other than white petals with the existing golden-yellow disk.
- Any vase glaze other than the existing blue or cream.
- Flowers in bowls/cups, liquids in vases, fruit in vases, or any new contents relationship.
- Multiple vases, independent scene objects alongside the flowers, bouquets with arbitrary counts, or arbitrary placement instructions.
- User-controlled flower order, stem positions, bloom heights, petal counts, leaf counts, or arrangement editing.
- New plant families, generic botanical grammar, flower pots, soil, gardens, trees, shrubs, grass, or outdoor scenes.
- New desktop panels, controls, camera controls, or a visual editor.
- Downloaded images, textures, fonts, external assets, models, network services, or neural components.

## Project constraints

- Keep generation offline, CPU-first, non-neural, seeded, deterministic, and inspectable.
- Preserve the existing stack, target framework, package versions, solution/project boundaries, analyzer policy, and CI workflow unless a concrete blocker justifies a small compatible change.
- Keep Core independent of Skia, pixels, bitmaps, native resources, and desktop/UI types.
- Reuse the existing prompt → Core → scene construction → Skia renderer → PNG/save path.
- Preserve all existing prompts, parser contracts, subject tokens, safe filenames, image paths, and desktop session semantics.
- Preserve the accepted visual appearance and deterministic output of all existing single-flower scenes, especially the tulip and daisy.
- Add no package, project, framework, CI runner, permanent workflow job, downloaded image, stock texture, or external asset.
- Do not introduce a generic bouquet/plant framework, arbitrary flower collection, unrestricted composition engine, universal contents/relationship abstraction, scene graph, object registry, reflection-based dispatch, physics engine, or 3D renderer.
- Model the mixed arrangement explicitly and narrowly: one existing vase, one tulip, one daisy, fixed species/quantity, and one authored composition.
- Keep all new arrangement geometry normalized and derived from the existing vase profile; no canvas-pixel placement constants.
- Keep flower geometry/materials species-specific. Share only low-level helpers that are genuinely independent of species shape.
- Preserve the existing standalone `VaseFlower` and `VaseDaisy` representations and their current output paths wherever practical. Do not convert every flower into a broad nullable record merely to make the mixed case fit.

## Sequential implementation checklist

### 0. Inspect, verify prerequisites, and record the starting point

- [x] Start from latest `main`; record exact HEAD commit and confirm a clean working tree. After fetching and fast-forwarding this session branch, the actual starting HEAD is `863dd75` (`docs: add Phase 9 mixed tulip and daisy arrangement checklist`); the working tree was clean before these Phase 9 setup edits.
- [x] Verify Phase 8 is complete: inspect `.agents/todo-8.md` and `docs/daisy-vase-handoff.md` in full. All boxes are checked; implementation, visual review and timing read-back are recorded. Found two stale progress sentences contradicting the close-out; correcting only those status lines and retaining the evidence.
- [x] Verify the Phase 8 merge and post-merge Linux Release CI status from GitHub; record exact PR, merge commit, and run. PR [#51](https://github.com/Vraxo/Formloom/pull/51) merged as `c441a9a`; post-merge Linux Release CI [42452733510](https://github.com/Vraxo/Formloom/actions/runs/42452733510) succeeded on that commit.
- [x] Inspect the current `VaseFlower`, `VaseDaisy`, `TulipPlant`, `DaisyPlant`, `VaseMouth`, and `VaseStem` representations. The two standalone content records each hold the same scene vase plus their species-specific plant; `VaseStem` is a renderer-only projection consumed by the single-stem mouth pass.
- [x] Trace existing standalone tulip and daisy prompts through parser, scene construction, validation, rendering, desktop session, preview metadata, and PNG save. Flow is exact parser → `SceneGenerator` → typed vase content/`SceneValidator` → `StillLifeRenderer` → `GenerationService` → `DesktopSession` preview/save metadata → whitelisted `PngSaveService` token.
- [x] Inspect the precise tulip and daisy geometry bounds and their measured bloom/stem/leaf dimensions for the default vase. Canonical seed 42/variation 0.35 sidecars: shared mouth center (0.49615, 0.22116), radii (0.04801, 0.02822). Tulip stem base-to-top 0.08129, thickness 0.00600; bloom 0.07686 × 0.08354; bounds 0.10306 × 0.16889; leaves about 0.05584 × 0.01240 and 0.05156 × 0.00981. Daisy stem base-to-top 0.13852, thickness 0.00408; bloom 0.10017 × 0.08614, disk 0.03371 × 0.02899; bounds 0.13893 × 0.19914; leaves about 0.07312 × 0.02194 and 0.06361 × 0.01908. All values are normalized scene units; per-leaf sizes derived from the canonical seeded geometry formulas.
- [x] Inspect the current flower draw order, flower clipping planes, vase body/rim occlusion, and stem continuation mask. Current order is flower-before-vase under the mouth-top clip, then ceramic layers, then one stem continuation through the lip-inset opening/column mask; current `DrawCeramic` accepts one `VaseStem?`.
- [x] Inspect the random-stream derivation and prove how each standalone species remains independent of vase glaze and the other species. Vase form uses `vase.form`; tulip uses `tulip.stem`, `tulip.leaves`, `tulip.bloom`; daisy uses `daisy.stem`, `daisy.leaves`, `daisy.petals`, `daisy.disk`. Color/glaze are material inputs; existing tests assert glaze independence and stream non-perturbation.
- [x] Inspect current prompt tests, renderer pixel/property tests, desktop pipeline tests, preview exporter, manifest, and artifact naming conventions. Routine artifact retention is sheets/manifests plus one canonical sample per family; exhaustive dumps remain behind `FORMFLOOM_EXPORT_ALL=1`.
- [x] Inspect the Phase 8 visual sheets and known limitations so the mixed arrangement addresses actual composition constraints rather than assumptions. The two 1280 × 640, 4×2 fixed-build sheets show both centered single blooms clear above the rim; the daisy head/bounds are wider and taller than the tulip. The vase opening is only 0.096 scene units wide, so layout must keep two bases separately inside it while the heads diverge above; the accepted variation-0 crowded leaf band and tilted high-variation mouth are cosmetic limits, not regressions.
- [ ] Record the smallest proposed Core representation, layout strategy, parser changes, and renderer order in the PR description before implementation.
- [x] Do not alter Phase 8 handoff evidence or existing single-flower documentation unless a real discrepancy is found. A concrete contradiction was found in the handoff/todo progress notes claiming timing and visual review were still open; this PR corrects only those status statements and adds the verified PR #51 close-out provenance, without changing accepted design or review evidence.

### 1. Add a narrow typed mixed-vase arrangement

- [ ] Add one explicit typed representation for exactly one tulip plus exactly one daisy associated with one existing vase.
- [ ] Use the smallest compatible model shape after inspecting the current Core types; a dedicated `VaseBouquet` or similarly explicit value is acceptable, but it must not become an arbitrary flower collection.
- [ ] The mixed arrangement must reference the same `CeramicInstance` already held by the scene; do not duplicate the vase transform, seed, glaze, profile, or depth.
- [ ] Carry the existing tulip colour as exactly red or yellow.
- [ ] Carry the existing daisy appearance as exactly white petals with a golden-yellow disk.
- [ ] Reuse the existing species-specific `TulipPlant` and `DaisyPlant` geometry models, or an equally explicit representation that preserves their distinct geometry and materials.
- [ ] Keep the tulip and daisy as typed vase contents, not separate `SceneEntry` neighbors.
- [ ] Keep the vase itself as the single independent scene entry; do not count the two flowers as additional scene objects.
- [ ] Keep the mixed arrangement mutually exclusive with standalone `VaseFlower`, standalone `VaseDaisy`, `BowlContainment`, and `CupContents`.
- [ ] Validate exactly one blue/cream vase, exactly one tulip, exactly one daisy, and no additional independent scene entries.
- [ ] Reject undefined enum values, missing plant geometry, invalid dimensions, invalid leaf/petal counts, non-finite coordinates, and contradictory content relationships.
- [ ] Reject incomplete mixed arrangements; never silently drop one species and render only the other.
- [ ] Preserve standalone `VaseFlower` and `VaseDaisy` APIs/semantics where practical; avoid broad nullable fields and unsafe casts.
- [ ] Keep Core values independent of Skia paints, shaders, colours, paths, and native resources.
- [ ] Keep validation errors explicit and specific enough to identify which half of the arrangement is invalid.

### 2. Design one fixed, balanced two-flower layout

- [ ] Derive both stem bases from the same vase profile's `MouthCenter`, `MouthRadius`, and `RimThickness`.
- [ ] Place the two stem bases at distinct positions inside the usable mouth opening; neither may start on or outside the exterior lip.
- [ ] Prove both bases fit the usable opening simultaneously, including stem thickness and any antialiasing/clip allowance.
- [ ] Derive both plant heights and bloom positions from the existing vase headroom and safety margins.
- [ ] Use one fixed authored arrangement: one tulip and one daisy, with stable left/right identity and a documented relative height/depth relationship.
- [ ] Select which species sits left/right and which bloom sits slightly higher/lower based on the actual geometry dimensions; document why the chosen arrangement balances the two distinct silhouettes.
- [ ] Keep the daisy's wider radial head and the tulip's narrower cupped head visibly separate.
- [ ] Ensure the two bloom bounds do not materially overlap at default seed/variation; allow only deliberate, visually clear overlap if the visual design truly requires it.
- [ ] Ensure the tulip's cupped head does not cover the daisy's golden disk.
- [ ] Ensure the daisy petals do not obscure the tulip enough to lose its recognizable cupped silhouette.
- [ ] Ensure both stems remain individually traceable from their respective blooms to the vase opening.
- [ ] Prevent stem crossings or, if a short crossing is required for the authored arrangement, document the depth order and prove it reads cleanly.
- [ ] Place exactly two leaves on each stem, for four total; keep each pair attached to its own stem.
- [ ] Balance the leaves so they do not merge into a single green mass or hide either bloom.
- [ ] Keep the leaves away from the rim and vase exterior; no leaf should appear to grow out of the ceramic wall.
- [ ] Reuse the existing tulip and daisy species-specific geometry profiles; do not flatten both species into one shared bloom/petal algorithm.
- [ ] Use a small, explicit arrangement profile with bounded seeded offsets rather than an unrestricted optimizer or collision engine.
- [ ] Use one named arrangement stream (or equivalent) for bounded composition jitter, independent of each species' own geometry streams.
- [ ] Ensure flower layout randomness cannot change species identity, flower count, petal count, leaf count, or the fixed left/right assignment.
- [ ] Ensure vase glaze changes do not change either plant's geometry.
- [ ] Ensure tulip red/yellow colour changes only the tulip material, not the daisy or arrangement geometry.
- [ ] Keep the full arrangement inside canvas safety margins at all supported dimensions, seeds, and variation values.
- [ ] Test geometry bounds, opening fit, bloom separation, stem separation, and leaf attachment in Core before rendering.

### 3. Render both flowers with correct depth and vase occlusion

- [ ] Extend the existing CPU Skia renderer and retain the current renderer entry point and PNG encoder.
- [ ] Draw both plants behind the vase body/rim using the established profile-aware flower clipping convention.
- [ ] Preserve the existing vase body, cavity, far rim, near lip, contour, foot, glaze, highlights, and shadow exactly outside intended stem/mouth overlap.
- [ ] Render both stems entering the same mouth, with each continuation clipped to the opening and correctly darkened by the vase cavity.
- [ ] Ensure both stems remain visible inside the opening without being merged into one thick stroke.
- [ ] Preserve the near-lip occlusion: neither stem may paint across the front lip or leak onto the vase exterior.
- [ ] Choose and document a deterministic per-plant draw/depth order so the stems, leaves, tulip petals, daisy petals, and disk overlap predictably.
- [ ] Keep the daisy's white petals and golden disk rendered by `DaisyMaterial`; keep red/yellow tulip petals rendered by `TulipMaterial`.
- [ ] Keep the shared green material or helpers only where the current species already intentionally shares them; do not cause one species' material changes to affect the other.
- [ ] Ensure the daisy disk remains visible and centred; ensure the tulip's overlapping petals still read as a cup.
- [ ] Ensure no petal, leaf, or stem pixels leak outside the expected union of both plant bounds and the intentional mouth region.
- [ ] Ensure the combined flower arrangement does not add an unintended tabletop cast shadow or alter the vase's existing shadow mask.
- [ ] Extend silhouette diagnostics to include both blooms, both stems, and all four leaves according to the established convention.
- [ ] Extend detail-omission diagnostics for both species without changing pixels outside their own geometry.
- [ ] Ensure diagnostic passes do not contaminate subsequent normal rendering.
- [ ] Preserve deterministic pixel output for identical inputs on the same supported runtime.
- [ ] Keep CPU cost reasonable; avoid full-canvas temporary surfaces, extra blur passes, or repeated expensive path reconstruction without evidence that they are needed.

### 4. Extend strict prompt parsing, generation, and save behavior

- [ ] Add exactly the four prompts listed under **New mixed-arrangement prompts**.
- [ ] Parse the two flowers as one typed arrangement associated with one vase, not as two independent objects plus a vase.
- [ ] Preserve existing case, whitespace, and terminal-punctuation normalization.
- [ ] Reject reversed flower order, reversed container wording, alternate relationship words, missing species or colours, unsupported colours, extra adjectives, extra objects, plurals, quantities, and malformed separators.
- [ ] Reject two tulips, two daisies, three flowers, two vases, or flower/container combinations outside this exact scope.
- [ ] Ensure invalid mixed prompts are rejected whole and cannot fall back to a single tulip, single daisy, empty vase, or ordinary mixed-object scene.
- [ ] Route all four prompts through the existing generation service, renderer, desktop session, preview, metadata, and PNG save path.
- [ ] Add four deterministic subject tokens and safe whitelisted PNG filenames for the four combinations.
- [ ] Preserve every existing fruit, ceramic, group, bowl, cup, tulip, and daisy token/filename unchanged.
- [ ] Preserve last-successful-image metadata when controls are edited without regenerating.
- [ ] Preserve error behavior: failed generation retains the previous successful preview and its corresponding seed, variation, subject token, and save metadata.
- [ ] Update concise prompt-help text with one mixed-arrangement example; do not turn the UI into a long prompt catalog.
- [ ] Add no new control, panel, arrangement editor, or workflow step.

### 5. Add Core, geometry, parser, and validation tests

- [ ] Test all four tulip-colour × vase-glaze mixed prompts.
- [ ] Test exact parsing, normalization, case, repeated whitespace, and terminal punctuation.
- [ ] Test that the mixed arrangement carries one tulip of the requested colour and one white/golden daisy.
- [ ] Test that the vase reference in the arrangement is the exact same instance held by the scene.
- [ ] Test that the two plants are contents of one vase and not independent `SceneEntry` objects.
- [ ] Test mutual exclusion with standalone tulip, standalone daisy, bowl containment, and cup contents.
- [ ] Test rejection of missing/duplicate/non-vase containers and extra independent scene entries.
- [ ] Test rejection of undefined flower kind, colour, appearance, invalid geometry, missing tulip, missing daisy, and duplicated species.
- [ ] Test rejection of all unsupported grammar variants, alternative word order, unsupported colours/species, quantities, plurals, extra adjectives/objects, and missing table phrase.
- [ ] Test that invalid mixed prompts cannot silently degrade to a valid single-flower or empty-vase scene.
- [ ] Test that both stem bases lie inside the usable vase mouth after accounting for stem radius.
- [ ] Test that both stems clear the rim and their continuation paths remain inside the opening.
- [ ] Test exactly two blooms, two stems, and four leaves in the arrangement.
- [ ] Test that the tulip retains its existing petal count and the daisy retains its twelve-petal/fixed-disk structure.
- [ ] Test that each leaf is attached to its own stem and the two stems are distinct.
- [ ] Test the fixed left/right assignment and deterministic bounded layout offsets.
- [ ] Test that bloom bounds are separated by the documented minimum clearance or the explicitly documented allowed overlap.
- [ ] Test that neither flower's geometry extends beyond normalized canvas bounds at supported seeds/variations/dimensions.
- [ ] Test all geometry for finite coordinates, valid dimensions, and stable bounds.
- [ ] Test deterministic geometry for identical inputs.
- [ ] Test alternate seeds and variation extremes change only bounded shape/layout details, not flower identity, counts, or vase fit.
- [ ] Test vase glaze independence for both plant geometries.
- [ ] Test that red versus yellow changes only tulip colour/material, not daisy appearance or placement.
- [ ] Test that new arrangement streams do not perturb standalone tulip or daisy geometry.
- [ ] Add a single-flower regression test using the pre-Phase-9 canonical output or a reproducible before/after comparison to prove standalone tulip and daisy outputs remain unchanged.
- [ ] Retain all existing parser, Core, geometry, validation, and relationship regression tests.

### 6. Add renderer pixel/invariant and desktop integration tests

- [ ] Test valid PNG output for all four mixed arrangements at 1024 × 1024.
- [ ] Test representative smaller, larger, square, and rectangular output dimensions already supported by the renderer.
- [ ] Test byte/pixel determinism for identical mixed-arrangement inputs on the same supported runtime.
- [ ] Add non-vacuous pixel assertions proving that both blooms, both stems, all four leaves, the daisy disk, and the tulip's petal structure are actually rendered.
- [ ] Assert that the two blooms are visually distinguishable and neither is mostly hidden by the other.
- [ ] Assert that the daisy's golden disk remains visible and the tulip remains recognizably cupped.
- [ ] Assert that both stems are visible above the rim and enter the same opening.
- [ ] Assert that both stem continuations are visible inside the opening and disappear behind the near lip.
- [ ] Assert that neither stem crosses the exterior vase wall or produces a detached green stub.
- [ ] Assert that no flower pixels leak onto the tabletop/background outside the measured plant bounds.
- [ ] Compare the mixed arrangement against the same empty vase and prove that changed pixels are confined to both plant regions plus intentional mouth overlap.
- [ ] Assert that the vase body, glaze, foot, and tabletop shadow remain unchanged outside the documented overlap.
- [ ] Assert that the combined arrangement adds no tabletop cast shadow and leaves the vase shadow mask unchanged.
- [ ] Assert that silhouette output includes both plants and that detail omission affects only the expected tulip/daisy details.
- [ ] Assert that diagnostic rendering followed by normal rendering matches a fresh normal render.
- [ ] Test all four combinations across supported sizes, seeds, and variation extremes for clipping, overlap collapse, leaf loss, or mouth-attachment defects.
- [ ] Retain all existing renderer invariants for fruit, ceramic, mixed scenes, bowl contents, cup contents, standalone tulip, and standalone daisy.
- [ ] Extend generation-service and desktop-session tests to submit all four mixed prompts through the real prompt-to-PNG path.
- [ ] Extend component tests to verify prompt submission, successful preview metadata, and save-subject metadata for both tulip colours.
- [ ] Test unsupported mixed prompts retain the previous successful preview and all corresponding metadata.
- [ ] Test all four new tokens/filenames and verify every existing safe filename remains unchanged.

### 7. Generate and review visual/performance artifacts

- [ ] Extend the existing `procedural-preview` artifact; do not add a second workflow or artifact pipeline without a demonstrated need.
- [ ] Generate default-seed/default-variation full-size PNGs for all four mixed combinations.
- [ ] Generate at least one alternate-seed sample for each tulip colour.
- [ ] Generate low- and high-variation examples for representative red and yellow arrangements.
- [ ] Generate silhouette diagnostics for representative mixed arrangements.
- [ ] Generate a compact review sheet and a manifest identifying each cell's prompt, tulip colour, daisy appearance, vase glaze, seed, variation, filename, and dimensions.
- [ ] Include individual full-resolution 1024 × 1024 samples alongside the review sheet.
- [ ] Keep generated files under ignored test-output/artifact directories; do not commit generated PNG binaries as source assets.
- [ ] Generate timing sidecars using the existing exporter conventions.
- [ ] Measure render-plus-PNG-encode for the mixed arrangement alongside the same empty vase, standalone tulip, and standalone daisy on the same runner/runtime.
- [ ] Record timing as environment-specific observations; do not claim a universal per-flower cost from one Stopwatch sample.
- [ ] Inspect actual generated pixels; generating/uploading a PNG alone is not visual review.
- [ ] Review that both species read immediately and distinctly at normal viewing size.
- [ ] Review that the overall arrangement feels intentionally composed, not like two flowers accidentally colliding.
- [ ] Review that the two blooms have clear silhouette separation and readable colour/material separation.
- [ ] Review that both stems enter the same vase naturally and remain visually distinct inside the mouth.
- [ ] Review all four leaves for attachment, balance, visibility, and lack of awkward bloom/rim intersections.
- [ ] Review red and yellow tulip variants against both blue and cream vases.
- [ ] Review alternate seeds and variation extremes for bloom collisions, stem crossings, clipping, broken petals, malformed leaves, or canvas-edge collisions.
- [ ] Review that the daisy disk and white petals remain legible next to both tulip colours.
- [ ] Review that the vase rim, mouth, body, glaze, foot, tabletop, and contact shadow have no regressions.
- [ ] Review legibility at the 320 px preview-sheet cell size; if a critical feature disappears at that size, treat it as a real usability issue rather than hiding it with a full-resolution crop.
- [ ] If actual pixels cannot be inspected, leave the human visual-review gate unchecked and list the exact artifact names, sheet dimensions/order, and manifest details. Do not fabricate a visual pass.
- [ ] Record cosmetic observations separately from defects and do not expand scope to unrelated appearance preferences.

### 8. Documentation, verification, and handoff

- [ ] Update the README current-capability section to mention one tulip plus one daisy sharing one existing ceramic vase.
- [ ] Document the exact four mixed-arrangement prompts and their strict grammar.
- [ ] Document the fixed composition: one red/yellow tulip, one white/golden daisy, two stems, four leaves, and one blue/cream vase.
- [ ] Document that single-tulip and single-daisy prompts remain supported independently and unchanged.
- [ ] Document that same-species pairs, extra flowers, arbitrary bouquets, new species, and other containers remain unsupported.
- [ ] Update the architecture overview only as much as needed to explain the narrow mixed arrangement and its renderer ordering.
- [ ] Add `docs/mixed-vase-arrangement-handoff.md` with starting/ending commits, Phase 8 prerequisite evidence, design decisions, typed Core model, normalized layout, stem attachment, draw order, grammar, subject tokens, tests, CI evidence, artifacts, timing observations, visual-review status, and known limitations.
- [ ] Keep `.agents/todo-9.md` updated throughout implementation.
- [ ] Mark a checkbox `[x]` only after the work is complete and the stated verification has actually been performed.
- [ ] Leave blocked, unexecuted, or human-review tasks unchecked and explain the blocker/evidence beside them.
- [ ] Run the established solution restore, Release warnings-as-errors build, and full solution test workflow through the existing CI gate.
- [ ] Confirm all existing and new test projects pass on the latest PR CI run.
- [ ] Confirm the post-merge Linux Release CI run passes before closing the milestone.
- [ ] Keep ordinary CI Release-only; do not add a permanent Debug job, Windows runner, package, project, or workflow solely for this milestone.
- [ ] Do not claim native Photino/WebView2 dialog, window-lifecycle, resize, or accessibility testing from headless tests or Linux CI.
- [ ] Record exact PR, merge commit, and passing CI run URLs in the handoff.
- [ ] Reconcile the checklist with the handoff and actual changed files; identify anything authored but not executed.
- [ ] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, visual-review status, artifact location, timing observations, and known limitations.

## Acceptance checklist

- [ ] Phase 8 is verified complete and its post-merge Linux Release CI evidence is recorded.
- [ ] All existing single-flower and non-flower prompts continue working unchanged.
- [ ] All four mixed tulip-and-daisy prompts parse and generate through the existing desktop prompt-to-PNG path.
- [ ] Core represents one explicit mixed arrangement associated with one existing vase, not three unrelated scene entries or an arbitrary flower collection.
- [ ] The arrangement contains exactly one tulip, one daisy, two stems, and four leaves.
- [ ] Both stems fit and enter the same vase opening naturally.
- [ ] The two blooms remain distinct, recognizable, and balanced at normal viewing size.
- [ ] The daisy retains its white radial petals and golden disk; the tulip retains its red/yellow cupped-petal appearance.
- [ ] The vase mouth/rim/body correctly occludes both hidden stem sections; neither stem leaks onto the exterior vase wall.
- [ ] Arrangement geometry is deterministic for identical inputs and bounded across seeds, variation, and supported output sizes.
- [ ] New arrangement randomness does not perturb standalone tulip/daisy geometry or any older scene output.
- [ ] The mixed arrangement adds no unintended tabletop shadow, vase-body repainting, exterior leakage, or canvas-edge collision.
- [ ] Automated Core/parser, geometry, renderer invariant, desktop integration, and all existing regression tests pass.
- [ ] Generated full-resolution samples and a correctly mapped review sheet are available; visual review is honestly recorded as passed or pending.
- [ ] README and handoff accurately document supported prompts, architecture, evidence, exclusions, and limitations.
- [ ] The completed change passes the repository's Linux Release CI gate and post-merge run.
- [ ] The agent stops and reports implementation decisions, actual test/CI evidence, sample artifacts, visual observations, and known limitations.
- [ ] Do not continue to larger bouquets, another flower species, another container type, or arbitrary arrangements without review.

## Explicitly out of scope

- Two tulips, two daisies, three or more flowers, or arbitrary flower counts.
- More flower species, colour variants, flower morphology, plant families, or general vegetation.
- Flowers in bowls/cups, liquids in vases, fruit in vases, or any additional containment relationship.
- Multiple vases, extra scene objects, arbitrary placement instructions, or a user-configurable bouquet editor.
- New vase profiles, glazes, table/background redesign, or unrelated material changes.
- Animated growth, wind, physics, botanical simulation, 3D, GPU rendering, neural models, or AI prompt translation.
- A general-purpose bouquet system, arbitrary typed flower collection, generic plant/material/relationship framework, scene graph, plugin architecture, or unrestricted natural-language parser.
- New packages, projects, frameworks, CI runners, permanent workflow jobs, or unrelated refactors.

## Working instruction

Implement this milestone directly in the existing repository, in small coherent steps, with tests as the system grows. Make reasonable engineering decisions without asking the user to design internal architecture. Do not stop after writing a plan or only documenting the work: complete the scoped implementation.

Preserve every existing generation path, especially the standalone tulip and daisy renderers. Use the existing vase profile as the source of truth for both stem bases and mouth occlusion. Reuse the two existing species-specific geometry models without flattening them into one generic flower type. Build one deterministic, fixed two-flower composition and keep it separate from any future general arrangement system. If the current model exposes a genuine blocker, explain the evidence and choose the smallest compatible alternative rather than broadening scope. Keep this checklist accurate throughout, stop at the acceptance checkpoint, and provide the complete handoff.
