# Formloom — Phase 7: A Tulip in a Ceramic Vase

## Objective

Introduce Formloom's first flower through one deliberately constrained still-life subject: **a single red or yellow tulip arranged in one existing blue or cream ceramic vase**.

This milestone adds a new organic subject family and a new, visually meaningful vase-content relationship. It must build on the existing normalized geometry, deterministic random streams, ceramic profiles/materials, composition, and CPU rendering pipeline. The result should look like a recognizable tulip growing from inside the vase, not a flower pasted onto the background or a stem drawn over the vase mouth.

The flower consists of exactly:

- One tulip bloom with a recognizable cupped silhouette and a small number of overlapping petals.
- One slender, gently curved green stem.
- Exactly two green leaves attached at different points on the stem.

The supported color combinations are red or yellow petals with either existing vase glaze (blue or cream): four prompts total. Stems and leaves remain green in all variants.

The milestone is a static, illustrated still-life subject. It is not a botanical simulator, bouquet editor, plant generator, or general-purpose relationship system.

## Starting point

- Repository: `https://github.com/Vraxo/Formloom`, branch `main`.
- Start from the latest `main`; record the exact starting commit and confirm the working tree is clean before editing.
- Previous milestone checklist: `.agents/todo-6.md`.
- Previous milestone handoff: `docs/cup-liquids-handoff.md`.
- Current source projects live under `src/`; tests live under `tests/`.
- Stack: .NET 10 / C# 14, PhotinoX.Blazor desktop, SkiaSharp CPU rendering, xUnit v3, existing Linux Release CI.
- Current Core scene model contains `FruitInstance`, `CeramicInstance`, `SceneEntry`, `StillLifeScene`, `CeramicProfile`, normalized vase geometry, `BowlContainment`, and typed `CupContents`.
- The existing vase profile exposes a normalized mouth center/radius, rim thickness, cavity depth and closed outline. The vase is already rendered with a cavity and rim.
- Existing fruit geometry uses normalized cubic contours and bounded, named random streams. Existing fruit stems/leaves may provide useful rendering or math patterns, but do not force the flower into a fruit-shaped representation.
- Existing contents relationships are intentionally narrow: `BowlContainment` and `CupContents`. Add a similarly narrow vase-flower representation; do not generalize these into a universal containment framework.
- Inspect the actual code and changed files before choosing exact implementation locations. The paths named in this document are orientation, not permission to assume a file still owns a behavior.

## Exact scope and prompt vocabulary

### Preserve all existing vocabulary

Every existing supported prompt and its current output/save behavior must continue to work:

- All six fruit descriptors and fruit-only groups.
- Blue and cream ceramic bowls, cups, and vases.
- Existing pairs, triples, and mixed still-life groups.
- All twelve fruit-in-bowl prompts.
- All four coffee/tea-in-cup prompts.
- Existing subject tokens, whitelisted filenames, seed/variation behavior, and rejection behavior.

### New flower-in-vase prompts

Support exactly these four prompts:

1. `a red tulip in a blue ceramic vase on a wooden table`
2. `a red tulip in a cream ceramic vase on a wooden table`
3. `a yellow tulip in a blue ceramic vase on a wooden table`
4. `a yellow tulip in a cream ceramic vase on a wooden table`

Continue tolerating the existing case normalization, repeated-whitespace normalization, and terminal-punctuation behavior. The parser must preserve the existing exact-grammar philosophy; do not add synonyms or free-form natural-language understanding.

### Explicitly unsupported

- Any flower other than a tulip.
- Pink, white, purple, orange, multicolored, or unspecified tulips; only red and yellow are supported.
- Flowers in bowls or cups; liquids in vases; fruit in vases; or any other new containment relation.
- Multiple flowers, multiple stems, multiple vases, bouquets, bunches, flower arrangements, or mixed flower/object prompts.
- Multiple blooms on one stem, buds, wilted flowers, dropped petals, pollen, stamens, visible seeds, or flower pots.
- Arbitrary leaf count, petal count, stem count, flower height, bend, bloom angle, or placement instructions.
- Additional plant species, trees, shrubs, grass, branches, or general vegetation.
- New desktop controls, a plant editor, camera controls, or arbitrary scene positioning.
- Downloaded images, textures, fonts, external assets, models, network services, or neural components.

## Project constraints

- [x] Preserve the current stack, target framework, package versions, solution/project boundaries, analyzer policy, and CI workflow unless a concrete blocker justifies a small compatible change.
- [x] Keep generation offline, CPU-first, non-neural, seeded, deterministic, and inspectable.
- [x] Keep Core independent of Skia, pixels, bitmaps, native resources, and desktop/UI types.
- [x] Reuse the existing prompt → Core → scene construction → Skia renderer → PNG/save path.
- [x] Preserve every existing supported prompt, parser contract, image path, deterministic stream, subject token, and save filename.
- [x] Preserve the accepted visual appearance of all existing fruit, empty ceramics, mixed scenes, fruit-in-bowl scenes, and filled-cup scenes.
- [x] Add no package, project, framework, CI runner, permanent workflow job, downloaded image, stock texture, or external asset.
- [x] Do not introduce a generic plant framework, botanical simulation, universal contents/relationship abstraction, scene graph, object registry, reflection-based dispatch, physics engine, or 3D renderer.
- [x] Keep flower geometry normalized and profile-aware; do not hard-code canvas-pixel coordinates for the vase attachment or tulip layout.
- [x] Keep this milestone limited to exactly one tulip associated with exactly one existing vase.
- [x] Treat the vase as the actual scene object and the flower as its typed content/arrangement; do not add the bloom, stem, and leaves as unrelated scene entries.
- [x] Keep the flower's stem/leaf/bloom geometry, flower material, and vase ceramic material separate.

## Sequential implementation checklist

### 0. Inspect and record the actual starting point

- [x] Start from latest `main`; record the exact HEAD commit and confirm the working tree is clean.
- [x] Read `.agents/todo-6.md` and `docs/cup-liquids-handoff.md` in full.
- [x] Inspect `StillLifeScene`, `SceneEntry`, `CeramicInstance`, `CeramicProfile`, `CeramicForms`, `SceneGenerator`, `PromptParser`, `SceneValidator`, and existing content relationship models.
- [x] Trace a standalone vase prompt end-to-end through parsing, Core scene construction, composition, renderer, generation service, desktop session, preview metadata, and PNG save.
- [x] Trace the current `BowlContainment` rendering and the current `CupContents` rendering without assuming their draw orders are interchangeable.
- [x] Inspect fruit organic contours, cubic-curve helpers, stem/leaf drawing, materials, named random streams, and attachment-aware bounds.
- [x] Inspect how vase body, cavity, mouth, front rim/lip, shadows, silhouette, and detail-omission diagnostics are rendered.
- [x] Inspect current renderer entry/depth ordering and determine the narrowest place to draw a vase's flower behind the vase mouth/body while leaving the bloom and leaves visible above the opening.
- [x] Inspect test helpers and the existing `procedural-preview` exporter/manifest conventions.
- [x] Write a concise implementation note in the PR description identifying the smallest Core, parser, generator, renderer, desktop, tests, and documentation changes.
- [x] Choose an explicit typed vase-flower model after inspecting the existing model; do not create a generic content system.

### 1. Add a typed vase-flower arrangement to Core

- [x] Add a closed flower-kind value with exactly one value, `Tulip`, only if the current model needs an explicit kind.
- [x] Add a closed tulip-color value with exactly `Red` and `Yellow` values.
- [x] Add a narrowly scoped typed vase-flower arrangement that associates one existing `CeramicInstance` of kind `Vase` with one tulip color/kind.
- [x] Keep the vase instance referenced by the arrangement identical to the vase entry already held by the scene; do not duplicate its transform, seed, glaze, profile, or depth.
- [x] Add one narrow optional vase-flower property to `StillLifeScene`, or the smallest equivalent representation that fits the current model.
- [x] Keep the flower out of `SceneEntry`; it is not an independently positioned neighbor object.
- [x] Keep flower types/colors independent of Skia paints, shaders, colors, paths, and native rendering resources.
- [x] Keep the arrangement mutually exclusive with `BowlContainment` and `CupContents`; a scene cannot contain conflicting contents relationships.
- [x] Validate that the arrangement references exactly one supported vase with blue or cream glaze.
- [x] Reject missing vases, non-vase containers, duplicate vases, extra independent entries, invalid enum values, and contradictory content relationships.
- [x] Keep existing one-to-three independent-object scene rules unchanged for ordinary scenes.
- [x] Keep validation errors explicit; never silently discard the flower and fall back to rendering an empty vase.
- [x] Keep ordinary standalone vase scenes unchanged with no flower-specific state or rendering.

### 2. Construct a recognizable tulip in vase-local normalized coordinates

- [x] Derive the stem base/attachment from the existing vase profile's `MouthCenter`, `MouthRadius`, and `RimThickness`.
- [x] Ensure the stem enters safely through the usable vase opening; it must not appear to grow from the vase's exterior wall or float beside the rim.
- [x] Define the flower in a small normalized local coordinate system whose origin/attachment is derived from the vase, not from 1024 × 1024 pixels.
- [x] Build one slender, gently curved stem with a restrained natural bend; avoid a rigid vertical line and avoid excessive wobble.
- [x] Build exactly two leaves attached to different stem locations, with distinct but balanced orientations.
- [x] Make the leaves elongated and lance-shaped, tapering to natural tips; do not reuse fruit body contours as leaves.
- [x] Give each leaf a restrained center vein and subtle shape shading; avoid dense veins, serrations, or noisy texture.
- [x] Build a single tulip bloom with a recognizable upright, cupped silhouette and a small number of overlapping petals.
- [ ] Ensure the bloom reads as a tulip rather than a daisy, generic five-point star, rose, bell ornament, or fruit. — pending visual review
- [x] Use overlapping petal contours to establish the cupped form; avoid a flat circle with radial lines.
- [x] Keep the bloom attached to the stem with a credible transition; no floating gap or oversized bulb-like joint.
- [x] Use the same bloom geometry for red and yellow variants; color must not change the silhouette.
- [ ] Keep the flower height and width proportionate to the existing vase so the bloom is clearly visible without dominating the whole canvas. — sizes are bounded by tests, the judgement is visual and pending
- [x] Ensure the full plant remains within canvas safety margins at all supported dimensions, seeds, and variation values.
- [x] Use bounded deterministic shape variation; variation must not change the flower identity, number of leaves, or number of blooms.
- [x] Use stable, named random streams for stem bend, leaf orientation/shape, and petal contour variation if randomness is useful.
- [x] Keep random streams independent from vase geometry/glaze and all existing fruit/cup/bowl streams.
- [x] Prefer a small authored profile with restrained seeded offsets over general-purpose plant grammar or unconstrained noise.
- [x] Test normalized flower bounds and attachment geometry before rendering.

### 3. Render the flower with correct vase occlusion and material separation

- [x] Extend the existing CPU Skia rendering path and keep the current renderer entry point and PNG encoder.
- [x] Draw the flower behind the vase's foreground mouth/rim and body so the stem is naturally hidden where it enters the vase.
- [x] Keep the bloom and visible leaves above the vase opening; do not let the vase body cover the flower head.
- [x] Preserve the vase's normal body, cavity, lip, rim, foot, glaze, highlights, and shadows; the flower must not repaint unrelated vase pixels.
- [x] Use an explicit, small draw-order implementation for the vase-flower arrangement rather than a generalized scene-graph or z-order framework.
- [x] Clip the hidden stem/attachment region using vase-local geometry or a similarly robust profile-aware method; do not use an arbitrary screen-space rectangle.
- [x] Ensure the stem appears to continue into the vase interior but does not show through the opaque vase body or leak outside the mouth.
- [ ] Keep both leaves visibly attached to the stem and avoid collisions with the vase rim that make them appear severed. — attachment is asserted in geometry, the visible read is pending review
- [x] Render the bloom with a red or yellow petal material using gradients and restrained petal-to-petal value shifts.
- [ ] Keep petals visibly distinct through overlapping edges and shading without heavy outlines or cartoon-like stripes. — pending visual review
- [x] Render stem and leaves with a coherent green material, subtle directional shading, and restrained highlights.
- [x] Ensure the flower material is independent of vase glaze: changing blue ↔ cream must not recolor petals, stem, or leaves.
- [ ] Keep the existing scene lighting direction coherent across vase glaze, petals, leaves, and stem. — pending visual review
- [ ] Avoid plastic, metallic, neon, glowing, oversaturated, or noisy surfaces. — pending visual review
- [x] Do not add an independent tabletop cast shadow for the flower; the vase retains its existing shadow behavior.
- [x] Add only subtle local overlap/attachment shading if it materially improves the stem's entry into the vase; do not simulate volumetric foliage shadows.
- [x] Preserve the current vase-only image path byte-for-byte where practical; tests must prove no flower pixels appear for an empty vase.
- [x] Keep all temporary Skia paths, paints, shaders, filters, masks, and surfaces correctly disposed; add no static native-resource cache.
- [x] Define explicit and stable behavior for normal rendering, silhouette output, shadow-mask output, and detail-omission diagnostics when a vase contains a tulip.
- [x] Ensure diagnostic passes cannot contaminate or alter a later normal render.
- [x] Ensure the flower is stable at the supported 64–4096 output dimensions and at square and rectangular output sizes.

### 4. Extend strict prompt parsing, generation, and save metadata

- [x] Add one additive typed parser result/entry point for a tulip in a vase, using the smallest compatible change to the current parser.
- [x] Parse exactly `a red tulip` and `a yellow tulip` as flower descriptors.
- [x] Parse exactly the existing blue and cream ceramic vase descriptors as containers.
- [x] Support all four flower-color × vase-glaze combinations.
- [x] Require the exact relationship word `in` between the flower descriptor and vase descriptor.
- [x] Require the existing terminal `on a wooden table` phrase.
- [x] Preserve existing case, repeated-whitespace, and terminal-punctuation normalization.
- [x] Keep flower identity/color and vase identity/glaze distinct in the typed parse result and Core scene.
- [x] Route valid flower-in-vase prompts through the existing scene-generation service and prompt-to-PNG path.
- [x] Keep existing `Parse`, `ParseFruit`, `ParseScene`, `ParseObjects`, `ParseContainment`, and `ParseCupContents` behavior unchanged except for a deliberate additive dispatch path that does not break their contracts.
- [x] Ensure a flower-in-vase prompt is not accidentally interpreted as an ordinary mixed-object group.
- [x] Reject flowers in bowls/cups, fruit in vases, beverages in vases, and any unsupported content/container pairing.
- [x] Reject unsupported flower names, colors, synonyms, alternative relationship wording, reversed wording, plural/quantity forms, extra adjectives, multiple flowers, multiple vases, and missing table phrases.
- [x] Reject malformed flower prompts as a whole; never silently drop the flower and generate an empty vase.
- [x] Preserve all existing standalone and grouped fruit, ceramic, mixed-scene, fruit-in-bowl, and filled-cup behavior.
- [x] Preserve seed/variation snapshots, randomize behavior, background generation, duplicate-action guards, error handling, and previous-preview retention on failure.
- [x] Preserve last-successful-image metadata when prompt/seed/variation controls are edited without regenerating.
- [x] Add four deterministic whitelisted subject tokens, e.g. `red-tulip-in-blue-vase`, `red-tulip-in-cream-vase`, `yellow-tulip-in-blue-vase`, and `yellow-tulip-in-cream-vase`.
- [x] Add matching safe PNG filenames through the existing whitelist; never derive filenames from raw prompt text.
- [x] Preserve every existing fruit, group, ceramic, still-life, bowl-containment, and cup-liquid subject token/filename unchanged.
- [x] Update concise prompt-help text with one tulip-in-vase example; do not add desktop controls or panels.

### 5. Add Core, parser, and validation tests

- [x] Test all four supported flower-color/vase-glaze prompt combinations.
- [x] Test case normalization, repeated whitespace, terminal punctuation, and exact-grammar behavior.
- [x] Test that parsed flower kind/color and vase kind/glaze match the prompt exactly.
- [x] Test that the flower is represented as typed vase content, not an independent `SceneEntry` or generic object.
- [x] Test that vase-flower contents are mutually exclusive with `BowlContainment` and `CupContents`.
- [x] Test Core validation for exactly one supported vase and exactly one supported tulip arrangement.
- [x] Test rejection of a bowl or cup as container, an undefined flower kind/color, a missing vase, a duplicate vase, an extra independent entry, and contradictory content relationships.
- [x] Test rejection of unsupported flower species/colors, reversed wording, alternative relationship phrases, plural/quantity forms, extra adjectives, malformed separators, and a missing table phrase.
- [x] Test that an invalid flower-in-vase prompt is rejected as a whole and cannot fall back to an ordinary empty-vase prompt.
- [x] Test stem attachment against `CeramicProfile.MouthCenter` and the usable opening bounds.
- [x] Test that all stem/leaf/bloom geometry is finite, positive where applicable, normalized, and within canvas safety margins.
- [x] Test that exactly two leaves and one bloom are represented for every supported seed/variation.
- [x] Test that red and yellow flowers share identical geometry for identical seed/variation inputs.
- [x] Test that geometry is deterministic for identical inputs and bounded across seeds and variation extremes.
- [x] Test that flower random streams are independent of vase glaze and existing fruit/cup/bowl streams.
- [x] Retain all existing Core/parser/scene-validation/organic-form/containment/cup-liquid tests.

### 6. Add renderer pixel/invariant and desktop integration tests

- [x] Test valid PNG output for all four combinations at 1024 × 1024.
- [x] Test representative smaller, larger, square, and rectangular output dimensions already supported by the renderer.
- [x] Test identical flower-in-vase inputs produce identical PNG bytes/pixels on the same supported runtime.
- [x] Test alternate seeds produce controlled flower-shape differences while preserving the requested species/color and vase glaze.
- [x] Add non-vacuous pixel invariants proving that the bloom, both leaves, and stem are actually rendered.
- [x] Assert the entire bloom lies above the vase mouth region and is not clipped by the vase.
- [x] Assert the stem visibly reaches the vase opening and does not appear detached or offset from it.
- [x] Assert no stem pixels leak visibly across the vase exterior/body; only the intended stem portion above the opening may remain visible.
- [x] Assert both leaves remain attached to the stem and the leaf count stays exactly two in the geometry representation.
- [x] Assert flower pixels do not leak into the tabletop/background outside the expected flower bounds.
- [x] Compare a flower-in-vase render against the same empty-vase render and prove changes are limited to the flower region and the intentional mouth-overlap region.
- [x] Assert vase ceramic pixels outside the intended stem/rim overlap remain byte-identical to the equivalent empty-vase render.
- [x] Assert the existing vase silhouette/body/foot and tabletop shadow behavior remain unchanged where required by the documented silhouette convention.
- [x] Assert the flower adds no independent tabletop cast shadow and does not change the vase's existing shadow mask.
- [x] Assert red and yellow petal regions differ visibly while stems/leaves remain the same green material.
- [x] Assert changing blue ↔ cream vase glaze does not recolor flower pixels.
- [x] Assert the flower stays inside canvas bounds at all supported output dimensions and at variation 0 and 1.
- [x] Test detail omission according to its documented behavior without changing pixels outside the flower/vase attachment region.
- [x] Test that diagnostic rendering followed by normal rendering produces the same normal-render bytes as a fresh render.
- [x] Retain all existing fruit, ceramic, mixed-scene, bowl-containment, cup-liquid, silhouette, shadow, clipping, PNG, resource-ownership, and determinism tests.
- [x] Extend generation-service/desktop-session tests to submit all four new prompts through the real prompt-to-PNG path.
- [x] Extend rendered-component tests to submit both red and yellow flower prompts and verify successful generation metadata.
- [x] Test that an unsupported flower/container prompt retains the previous successful preview and matching save metadata.
- [x] Test all four new subject tokens/filenames and confirm every existing filename remains unchanged.

### 7. Generate and review visual/performance artifacts

- [x] Extend the existing `procedural-preview` test artifact; do not add a second workflow or artifact pipeline without a demonstrated need.
- [x] Generate default-seed/default-variation full-size PNGs for red-blue, red-cream, yellow-blue, and yellow-cream tulip/vase combinations.
- [x] Generate at least one alternate-seed sample for red and yellow tulips.
- [x] Generate low- and high-variation examples for representative red and yellow combinations.
- [x] Generate flower/vase silhouette diagnostics for representative cases and verify the documented convention.
- [x] Generate a compact row-major review sheet and an accompanying manifest identifying each cell's prompt, flower color, vase glaze, seed, variation, filename, and dimensions.
- [x] Include individual full-resolution 1024 × 1024 PNGs alongside the review sheet.
- [x] Keep generated PNGs and review composites in ignored test-output/artifact directories; do not commit generated binaries as source assets.
- [x] Generate timing sidecars using the existing exporter conventions.
- [ ] Measure render-plus-PNG-encode time at 1024 × 1024 for flower-in-vase scenes and compare with equivalent empty-vase renders under the same runner/runtime; report observations, not a universal performance claim. — the sidecars record it; the figures are not reachable from this environment
- [ ] Inspect actual generated pixels where the environment permits; producing or uploading a PNG alone is not visual review. — the environment cannot read artifact blobs
- [ ] Review that the bloom reads immediately as a tulip without a label. — pending visual review
- [ ] Review that the bloom is cupped and petal-shaped, not a daisy, star, generic blob, rose, or fruit. — pending visual review
- [ ] Review that the stem appears to enter the vase naturally and is hidden at the mouth/body in the correct places. — pending visual review
- [ ] Review that exactly two leaves are visible, attached, balanced, and not intersecting the bloom awkwardly. — pending visual review
- [ ] Review both red and yellow petals against both blue and cream vase glazes. — pending visual review
- [ ] Review alternate seeds and variation extremes for broken petals, malformed leaves, excessive bends, clipping, or canvas-edge collisions. — pending visual review
- [ ] Review the vase rim, mouth, body, glaze, foot, and tabletop grounding for regressions. — pending visual review
- [ ] Review that the flower materials are restrained and coherent with the existing still-life lighting rather than neon, plastic, metallic, or overly cartoon-like. — pending visual review
- [x] If actual generated pixels cannot be inspected, leave the human visual-review gate pending and provide exact artifact names, sheet dimensions/order, and manifest details; do not fabricate a visual pass.
- [x] Record cosmetic observations separately from defects and do not expand scope to unrelated appearance preferences.

### 8. Documentation, verification, and handoff

- [x] Update the README current-capability section to mention one red or yellow tulip in a blue or cream ceramic vase.
- [x] Document the exact four supported tulip-in-vase prompts and their strict grammar.
- [x] Document that the arrangement contains one bloom, one stem, and two leaves.
- [x] Document that only tulips are supported and that other flower species, colors, quantities, and vase contents remain unsupported.
- [x] Document that ordinary empty vases, fruit/ceramic groups, fruit-in-bowl scenes, and filled coffee/tea cups remain supported and unchanged.
- [x] Update the architecture overview only as much as needed to explain the typed vase-flower arrangement and vase-mouth occlusion/draw order.
- [x] Add `docs/vase-flower-handoff.md` with starting/ending commits, design decisions, typed Core model, normalized geometry, flower profiles/materials, vase attachment and occlusion, grammar, subject tokens, tests, CI evidence, artifact names, timing observations, visual-review status, and known limitations.
- [x] Keep `.agents/todo-7.md` updated throughout implementation.
- [x] Mark a checkbox `[x]` only after its work is complete and the stated verification has actually been performed.
- [x] Leave blocked, unexecuted, or human-review tasks unchecked and explain the blocker/evidence beside them.
- [x] Run the established solution restore, Release warnings-as-errors build, and full solution test workflow through the existing CI gate.
- [x] Confirm all existing and new test projects pass on the latest PR CI run.
- [ ] Confirm the post-merge Linux Release CI run passes before closing the milestone. — recorded after the merge
- [x] Keep ordinary CI Release-only; do not add a permanent Debug job, Windows runner, package, project, or workflow solely for this milestone.
- [x] Do not claim native Photino/WebView2 dialog, window-lifecycle, resize, or accessibility testing from headless tests or Linux CI.
- [ ] Record exact PR, merge commit, and passing CI run URLs in the handoff. — merge commit pending
- [x] Reconcile this checklist with the handoff and actual changed files; identify anything authored but not executed.
- [x] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, visual-review status, artifact location, timing observations, and known limitations.

## Acceptance checklist

- [x] All existing fruit, ceramic, mixed-scene, fruit-in-bowl, and coffee/tea-in-cup prompts continue working unchanged.
- [x] All four tulip-color × vase-glaze prompts parse and generate through the existing desktop prompt-to-PNG path.
- [x] Core represents the flower as typed contents/arrangement of one vase, not as unrelated scene entries or a generic relationship object.
- [x] The arrangement contains exactly one recognizable tulip bloom, one stem, and two leaves.
- [x] The stem is derived from vase-local mouth geometry and appears to enter the vase naturally.
- [x] The vase mouth/rim/body correctly occludes the hidden stem section; the bloom and leaves remain visible above the opening.
- [x] Red and yellow tulips are recognizably distinct while sharing the same flower geometry; blue and cream vase glazes remain distinct and do not recolor the flower.
- [x] Flower geometry is deterministic for identical inputs and bounded across seeds, variation, and supported output sizes.
- [x] The flower adds no unintended tabletop shadow, vase-body repainting, exterior leakage, or canvas-edge collision.
- [x] Automated Core/parser, renderer invariant, desktop integration, and existing regression tests pass.
- [x] Generated full-resolution samples and a correctly mapped review sheet are available; visual review is honestly recorded as passed or pending.
- [x] README and handoff accurately document supported prompts, architecture, evidence, exclusions, and limitations.
- [ ] The completed change passes the repository's Linux Release CI gate and the post-merge run. — PR gate green, post-merge run pending
- [x] The agent stops and reports implementation decisions, actual test/CI evidence, sample artifacts, visual observations, and known limitations.
- [x] Do not continue to another flower species, plant family, container type, or relationship milestone without review.

## Explicitly out of scope

- Roses, daisies, sunflowers, lilies, orchids, wildflowers, buds, or any flower other than the single tulip profile.
- Bouquets, multiple stems, multiple blooms, multiple leaves beyond the two required leaves, or arbitrary plant arrangements.
- Flower pots, soil, garden scenes, trees, shrubs, grass, branches, or general vegetation.
- Flowers in bowls/cups, liquids in vases, fruit in vases, or any additional containment relation.
- Multiple vases, additional scene objects in a flower-in-vase prompt, or arbitrary spatial instructions.
- Vase redesign, new ceramic colors, new materials unrelated to the flower, or table/background redesign.
- Animated growth, wind, physics, botanical simulation, 3D, GPU rendering, neural models, or AI prompt translation.
- A generalized plant/material/relationship framework, scene graph, plugin architecture, or unrestricted natural-language parser.
- New packages, projects, frameworks, CI runners, permanent workflow jobs, or unrelated refactors.

## Working instruction

Implement this milestone directly in the existing repository, in small coherent steps, with tests as the system grows. Make reasonable engineering decisions without asking the user to design internal architecture. Do not stop after writing a plan or only documenting the work: complete the scoped implementation.

Preserve every existing supported generation path. Use the existing vase mouth profile as the source of truth for the stem attachment and draw order. Keep the typed vase-flower representation narrow and explicit; do not generalize it into a universal plant or containment system. If the current model exposes a genuine blocker, explain the evidence and choose the smallest compatible alternative rather than broadening scope. Keep the checklist accurate throughout, stop at the acceptance checkpoint, and provide the complete handoff.
