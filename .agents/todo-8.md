# Formloom — Phase 8: A Daisy in a Ceramic Vase

## Objective

Extend the existing vase-flower capability with one distinctly different flower species: **a single white daisy with a golden-yellow centre in one existing blue or cream ceramic vase**.

This milestone must prove that Formloom can add a second flower species without forcing the daisy into the tulip's cupped-petal geometry or creating a broad, speculative botanical framework.

The daisy consists of exactly:
- One open, radially arranged bloom.
- One slender, gently curved green stem.
- Exactly two green leaves attached at different stem locations.
- One golden-yellow central disk surrounded by a restrained ring of white petals.

The daisy must remain a typed vase-content arrangement, using the existing vase mouth, attachment, clipping, and occlusion conventions. The existing tulip must remain unchanged in geometry, appearance, prompts, filenames, and deterministic behavior.

## Starting point

- Repository: `https://github.com/Vraxo/Formloom`, branch `main`.
- Start from the latest `main`; record the exact starting commit and confirm a clean working tree.
- Previous milestone checklist: `.agents/todo-7.md`.
- Previous milestone handoff: `docs/vase-flower-handoff.md`.
- Source projects live under `src/`; tests live under `tests/`.
- Stack: .NET 10 / C# 14, PhotinoX.Blazor desktop, SkiaSharp CPU rendering, xUnit v3, existing Linux Release CI.
- Current flower-in-vase path supports one red or yellow tulip, one green stem, exactly two green leaves, and one blue or cream vase.
- Current Core model includes a typed `VaseFlower` arrangement and explicit `TulipPlant` geometry. Inspect the actual current code before deciding how the second species should be represented.
- Existing flower geometry is normalized, derived from vase-local mouth geometry, and uses named deterministic random streams.
- The current renderer draws the flower behind the vase mouth/body and then restores the stem continuation inside the opening. Preserve this carefully tested behavior.
- Paths and type names in this document are orientation, not permission to assume a file still owns a behavior. Trace the actual current implementation first.

## Exact scope and prompt vocabulary

### Preserve all existing vocabulary

Every existing supported prompt and behavior must continue working:
- All existing fruit descriptors, fruit groups, and mixed scenes.
- Blue and cream ceramic bowls, cups, and vases.
- Fruit-in-bowl containment.
- Coffee/tea-in-cup contents.
- Red/yellow tulip-in-blue/cream-vase prompts.
- Existing subject tokens, whitelisted filenames, seed/variation behavior, normalization, and rejection behavior.

### New daisy-in-vase prompts

Support exactly these two prompts:

1. `a white daisy in a blue ceramic vase on a wooden table`
2. `a white daisy in a cream ceramic vase on a wooden table`

Continue the existing case normalization, repeated-whitespace normalization, and terminal-punctuation behavior. Preserve the exact-grammar philosophy; do not add synonyms or free-form natural-language understanding.

### Explicitly unsupported

- Any flower species other than tulip and this single daisy profile.
- Other daisy petal colours, coloured centres, multicoloured petals, or unspecified flower colours.
- Multiple daisies, multiple blooms, bouquets, bunches, multiple stems, or additional leaves.
- Flowers in bowls/cups, liquids in vases, fruit in vases, or any new contents relationship.
- Arbitrary petal count, flower size, height, bend, bloom angle, leaf count, or placement instructions.
- New plant species, general vegetation, flower pots, soil, gardens, or outdoor scenes.
- Arbitrary flower arrangements or mixed daisy-plus-other-object prompts.
- New desktop controls, a flower editor, camera controls, or arbitrary positioning.
- Downloaded images, textures, fonts, external assets, models, network services, or neural components.

## Project constraints

- Keep generation offline, CPU-first, non-neural, seeded, deterministic, and inspectable.
- Preserve the existing stack, target framework, package versions, solution/project boundaries, analyzer policy, and CI workflow unless a concrete blocker justifies a small compatible change.
- Keep Core independent of Skia, pixels, bitmaps, native resources, and desktop/UI types.
- Reuse the existing prompt → Core → scene construction → Skia renderer → PNG/save path.
- Preserve every existing prompt, parser contract, image path, deterministic stream, subject token, and save filename.
- Preserve the accepted visual appearance of all existing fruit, ceramics, mixed scenes, fruit-in-bowl scenes, filled cups, and tulip-in-vase scenes.
- Add no package, project, framework, CI runner, permanent workflow job, downloaded image, stock texture, or external asset.
- Do not introduce a generic botanical framework, plant simulator, universal flower grammar, universal contents/relationship abstraction, scene graph, object registry, reflection-based dispatch, physics engine, or 3D renderer.
- Keep daisy geometry normalized and profile-aware; do not hard-code canvas-pixel coordinates for the vase attachment or bloom layout.
- Keep the daisy's stem, leaves, petals, central disk, flower material, and vase ceramic material separate.
- The daisy is exactly one bloom, one stem, and two leaves in one existing vase.

## Sequential implementation checklist

### 0. Inspect and record the actual starting point

- [ ] Start from latest `main`; record the exact HEAD commit and confirm the working tree is clean.
- [ ] Read `.agents/todo-7.md` and `docs/vase-flower-handoff.md` in full.
- [ ] Reconcile the Phase 7 handoff's stale `Authored but not executed` statements with the later post-fix artifact and visual review: verify the exact evidence, remove or rewrite contradictory claims, and distinguish the original pre-fix sheet from the later stem-continuation fix artifact. Do not overstate which full-resolution samples were inspected.
- [ ] Inspect the current `VaseFlower`, `FlowerKind`, `TulipColor`, `TulipPlant`, `TulipGeometry`, and `TulipMaterial` implementations.
- [ ] Trace a tulip prompt end-to-end through parsing, Core scene construction, validation, renderer, generation service, desktop session, preview metadata, and PNG save.
- [ ] Inspect the exact draw order for flower clipping, vase cavity, far rim, near lip, vase contour, and stem continuation.
- [ ] Inspect the current flower bounds, attachment invariants, random-stream derivation, silhouette diagnostics, detail-omission behavior, and procedural-preview exporter.
- [ ] Inspect current Core and renderer test conventions, including non-vacuous pixel assertions.
- [ ] Identify the smallest explicit model extension that can represent both tulip and daisy without weakening validation or breaking the tulip path.
- [ ] Record the intended files and design choice in the PR description before implementation; do not write a speculative architecture document.

### 1. Extend the typed flower-in-vase model narrowly

- [ ] Add daisy as an explicit supported flower kind/value, or use an equally narrow representation that fits the actual current model.
- [ ] Represent the daisy's fixed white-petal/golden-centre appearance explicitly; do not reuse `TulipColor` to imply daisy colour.
- [ ] Represent daisy geometry with a dedicated typed model (for example, a daisy plant/bloom/petal/disk representation) rather than pretending it is a `TulipPlant`.
- [ ] Keep the vase instance referenced by the arrangement identical to the vase entry already held by the scene; do not duplicate transform, seed, glaze, profile, or depth.
- [ ] Keep the flower out of `SceneEntry`; it remains typed vase content, not an independently positioned neighboring object.
- [ ] Preserve the existing tulip representation and validation semantics wherever possible.
- [ ] If the current `VaseFlower` shape must change, use an explicit closed representation with exhaustive validation; avoid an untyped object payload or string-based dispatch.
- [ ] Keep flower species and appearance values independent of Skia paints, shaders, colours, paths, and native rendering resources.
- [ ] Keep vase-flower contents mutually exclusive with `BowlContainment` and `CupContents`.
- [ ] Validate exactly one supported vase and exactly one valid flower arrangement.
- [ ] Reject undefined enum values, missing geometry, invalid petal/disk counts, non-finite coordinates, non-positive dimensions, and contradictory contents.
- [ ] Keep ordinary empty vase scenes and all existing tulip scenes unchanged.

### 2. Construct a recognizable daisy in vase-local normalized coordinates

- [ ] Derive the stem base/attachment from the existing vase profile's mouth centre, usable opening, and rim thickness.
- [ ] Ensure the stem enters safely through the usable vase opening and does not emerge from the exterior wall or float beside the rim.
- [ ] Define all daisy geometry in normalized scene/vase-local coordinates; do not hard-code 1024 × 1024 pixel positions.
- [ ] Build one slender, gently curved green stem with restrained natural bend.
- [ ] Build exactly two green leaves attached at different points on the stem.
- [ ] Make leaves elongated and lance-shaped, with tapered tips and restrained centre veins; reuse only genuinely shared leaf helpers if they fit both species cleanly.
- [ ] Build one open daisy bloom with a clearly radial, approximately symmetric arrangement around a distinct central disk.
- [ ] Use a fixed, modest petal count (choose and document a count appropriate to the desired silhouette); keep the count constant across seeds and variation.
- [ ] Give each petal a tapered attachment near the disk and a rounded or softly notched outer end; avoid generic identical oval spokes.
- [ ] Arrange petals with slight, bounded overlap and small authored differences in length, width, angle, and curvature so the bloom feels organic rather than like a flat gear.
- [ ] Ensure the petals visibly radiate from behind/around the central disk, with no gaps that make them look detached.
- [ ] Build a distinct golden-yellow central disk with a gently domed or softly textured appearance; do not use a flat yellow circle with no volume cues.
- [ ] Keep the disk visually centred over the petal roots and attached to the bloom; no floating gap or oversized button-like centre.
- [ ] Make the flower read immediately as a daisy at normal viewing size, not a sunflower, generic star, gear, pinwheel, or tulip.
- [ ] Keep the bloom orientation and stem transition botanically plausible for a simple illustrated still life.
- [ ] Keep the daisy proportionate to the existing vase; it must be clearly visible without overwhelming the vessel or canvas.
- [ ] Keep the entire flower inside safe canvas margins across supported sizes, seeds, and variation values.
- [ ] Use bounded deterministic variation for petal contour, rotation, stem bend, and leaf shape; variation must never change species identity, petal count, leaf count, or colour.
- [ ] Use stable named random streams for daisy stem, leaves, petals, and central disk if randomness is useful.
- [ ] Keep daisy streams independent of tulip geometry, vase geometry/glaze, and all fruit/cup/bowl streams.
- [ ] Prefer an authored daisy profile with restrained seeded offsets over unconstrained radial noise or a general flower grammar.
- [ ] Test normalized bounds, radial arrangement, disk centring, petal count, and vase attachment before rendering.

### 3. Render the daisy with correct materials and vase occlusion

- [ ] Extend the existing CPU Skia renderer and retain the current renderer entry point and PNG encoder.
- [ ] Draw the daisy behind the vase's foreground mouth/rim and body so the stem is naturally hidden where it enters the vase.
- [ ] Keep the bloom and visible leaves above the vase opening; do not let the vase body cover the bloom.
- [ ] Preserve the existing vase body, cavity, far rim, near lip, contour, foot, glaze, highlights, and shadows.
- [ ] Render white petals with subtle warm/cool shading and restrained edge separation; avoid pure flat white shapes that merge into highlights.
- [ ] Render the central disk in golden yellow with enough tonal variation to read as a distinct, gently domed centre.
- [ ] Keep stem and leaves green, with consistent lighting and restrained gradients.
- [ ] Clip all flower pixels to the expected flower region and vase-mouth overlap; prevent leakage onto the vase exterior, table, or background.
- [ ] Preserve the existing stem-continuation mask and ensure the daisy stem enters the vase with the same continuous visual treatment as the tulip.
- [ ] Keep the daisy's petal/disk rendering separate from the tulip petal material and bloom drawing; share only low-level helpers that are demonstrably shape-agnostic.
- [ ] Ensure the flower does not add a tabletop cast shadow or alter the vase's existing shadow mask unless the handoff explicitly justifies a scoped change.
- [ ] Preserve silhouette and detail-omission diagnostic semantics, extending them only as needed for daisy-specific geometry.
- [ ] Ensure diagnostic rendering cannot contaminate a subsequent normal render.
- [ ] Keep rendering CPU-friendly at 1024 × 1024 and representative supported sizes.
- [ ] Preserve deterministic pixel output for identical inputs on the same supported runtime.

### 4. Extend the strict prompt, generation, and save path

- [ ] Add exactly `a white daisy in a blue ceramic vase on a wooden table`.
- [ ] Add exactly `a white daisy in a cream ceramic vase on a wooden table`.
- [ ] Preserve existing case, whitespace, and terminal-punctuation normalization.
- [ ] Reject all other daisy colours, flower species, quantities, vase/container combinations, extra objects, and unsupported relationship wording.
- [ ] Ensure invalid daisy prompts cannot silently fall back to an empty vase or a tulip.
- [ ] Route both daisy prompts through the same generation service, renderer, desktop session, preview, and PNG save path as existing scenes.
- [ ] Add two deterministic subject tokens and safe whitelisted PNG filenames for the two daisy/vase combinations.
- [ ] Preserve every existing tulip token/filename and every earlier subject token/filename unchanged.
- [ ] Preserve last-successful-image metadata when prompt/seed/variation controls are edited without regenerating.
- [ ] Do not add desktop controls, panels, or a flower editor; existing prompt input is sufficient.
- [ ] Update concise prompt-help text with one daisy-in-vase example without listing an unbounded vocabulary.

### 5. Add Core, parser, and validation tests

- [ ] Test both supported daisy/vase-glaze prompt combinations.
- [ ] Test case normalization, repeated whitespace, terminal punctuation, and exact-grammar behavior.
- [ ] Test that parsed daisy kind, fixed appearance, and vase kind/glaze match the prompt.
- [ ] Test that the daisy is represented as typed vase content, not an independent `SceneEntry` or generic object.
- [ ] Test that daisy contents are mutually exclusive with bowl containment and cup contents.
- [ ] Test validation for exactly one supported vase and exactly one valid daisy arrangement.
- [ ] Test rejection of missing vase, non-vase container, duplicate vase, extra independent entries, undefined kind/value, and contradictory relationships.
- [ ] Test rejection of unsupported flower colours/species, reversed wording, alternative relationship phrases, plural/quantity forms, extra adjectives, malformed separators, and missing table phrase.
- [ ] Test that invalid daisy prompts are rejected as a whole and cannot fall back to an empty vase or tulip.
- [ ] Test stem attachment against the vase mouth's usable opening bounds.
- [ ] Test that all stem/leaf/petal/disk geometry is finite, positive where applicable, normalized, and within canvas safety margins.
- [ ] Test that exactly two leaves, one bloom, and the documented fixed petal count are represented across seeds and variations.
- [ ] Test radial petal placement and central-disk centring with geometric assertions, not only enum comparisons.
- [ ] Test deterministic geometry for identical inputs and bounded variation across seeds and variation extremes.
- [ ] Test that changing the seed can produce controlled shape differences without changing flower identity or petal/leaf counts.
- [ ] Test daisy stream independence from tulip, vase glaze, and existing fruit/cup/bowl streams.
- [ ] Test that changing vase glaze does not change daisy geometry.
- [ ] Retain all existing Core/parser/scene-validation/organic-form/containment/cup-liquid/tulip tests.

### 6. Add renderer pixel/invariant and desktop integration tests

- [ ] Test valid PNG output for both daisy/vase combinations at 1024 × 1024.
- [ ] Test representative smaller, larger, square, and rectangular output dimensions already supported by the renderer.
- [ ] Test identical daisy inputs produce identical PNG bytes/pixels on the same supported runtime.
- [ ] Test alternate seeds produce controlled differences while preserving daisy identity and vase glaze.
- [ ] Add non-vacuous pixel invariants proving that white petals, yellow disk, both leaves, and stem are actually rendered.
- [ ] Assert the bloom and central disk are above the vase mouth and are not clipped by the vase.
- [ ] Assert the stem visibly reaches and enters the vase opening without detachment or exterior-wall leakage.
- [ ] Assert the petals remain connected visually around the disk and do not collapse into a star/gear-like silhouette at supported sizes.
- [ ] Compare a daisy-in-vase render against the same empty-vase render and prove changes are limited to the flower region and intentional mouth-overlap region.
- [ ] Assert vase ceramic pixels outside the intended stem/rim overlap remain identical to the equivalent empty-vase render where the existing convention requires it.
- [ ] Assert the daisy adds no unintended tabletop shadow and does not change the vase's existing shadow mask.
- [ ] Assert white petal pixels and yellow disk pixels are distinguishable from each other and from vase highlights.
- [ ] Assert flower pixels do not leak onto the table/background outside expected flower bounds.
- [ ] Assert the flower remains inside canvas bounds at all supported output dimensions and variation 0 and 1.
- [ ] Test silhouette and detail-omission diagnostics according to their documented behavior.
- [ ] Test diagnostic rendering followed by normal rendering produces the same normal-render bytes as a fresh render.
- [ ] Retain all existing fruit, ceramic, mixed-scene, bowl-containment, cup-liquid, tulip, silhouette, shadow, clipping, PNG, resource-ownership, and determinism tests.
- [ ] Extend generation-service/desktop-session tests to submit both daisy prompts through the real prompt-to-PNG path.
- [ ] Extend rendered-component tests to verify successful generation metadata for both daisy prompts.
- [ ] Test that an unsupported daisy prompt retains the previous successful preview and matching save metadata.
- [ ] Test both new subject tokens/filenames and confirm every existing filename remains unchanged.

### 7. Generate and review visual/performance artifacts

- [ ] Extend the existing `procedural-preview` artifact; do not add a second workflow or artifact pipeline without a demonstrated need.
- [ ] Generate default-seed/default-variation full-size PNGs for both daisy/vase combinations.
- [ ] Generate at least one alternate-seed sample for each vase glaze.
- [ ] Generate low- and high-variation examples for a representative daisy/vase combination.
- [ ] Generate flower/vase silhouette diagnostics for representative cases and verify the documented convention.
- [ ] Generate a compact review sheet and manifest identifying each cell's prompt, vase glaze, seed, variation, filename, and dimensions.
- [ ] Include individual full-resolution 1024 × 1024 PNGs alongside the review sheet.
- [ ] Keep generated PNGs and review composites in ignored test-output/artifact directories; do not commit generated binaries as source assets.
- [ ] Generate timing sidecars using the existing exporter conventions.
- [ ] Measure render-plus-PNG-encode time at 1024 × 1024 for daisy-in-vase scenes and compare with equivalent empty-vase and tulip-in-vase renders on the same runner/runtime.
- [ ] Report timing observations as measurements from that environment, not universal performance claims.
- [ ] Inspect actual generated pixels where the environment permits; generating or uploading a PNG alone is not visual review.
- [ ] Review that the flower reads immediately as a daisy without a label.
- [ ] Review that the bloom has a clear radial petal arrangement and a distinct golden centre, not a sunflower, gear, star, pinwheel, or tulip.
- [ ] Review that the stem enters the vase naturally and is hidden at the mouth/body in the correct places.
- [ ] Review that exactly two leaves are visible, attached, balanced, and not awkwardly intersecting the bloom.
- [ ] Review both blue and cream vase combinations and confirm the white/yellow flower remains legible against each.
- [ ] Review alternate seeds and variation extremes for broken petals, uneven gaps, malformed leaves, excessive bends, clipping, or canvas-edge collisions.
- [ ] Review vase rim, mouth, body, glaze, foot, and tabletop grounding for regressions.
- [ ] Review that flower materials fit the existing illustrated still-life lighting and are not flat, neon, plastic, or excessively noisy.
- [ ] If actual generated pixels cannot be inspected, leave the human visual-review gate pending and provide exact artifact names, sheet dimensions/order, and manifest details; do not fabricate a visual pass.
- [ ] Record cosmetic observations separately from defects and do not expand scope to unrelated appearance preferences.

### 8. Documentation, verification, and handoff

- [ ] Update the README current-capability section to mention one white daisy in a blue or cream ceramic vase.
- [ ] Document the exact two supported daisy prompts and their strict grammar.
- [ ] Document that the daisy contains one bloom, one stem, exactly two leaves, white petals, and a golden-yellow centre.
- [ ] Document that tulips remain red or yellow and that daisy colours/species/quantities beyond this milestone remain unsupported.
- [ ] Document that empty vases, tulip-in-vase scenes, fruit/ceramic groups, bowl containment, and filled coffee/tea cups remain supported.
- [ ] Update the architecture overview only as much as needed to explain the explicit second flower representation and its draw order.
- [ ] Add `docs/daisy-vase-handoff.md` with starting/ending commits, design decisions, typed Core representation, normalized geometry, materials, vase attachment/occlusion, grammar, subject tokens, tests, CI evidence, artifact names, timing observations, visual-review status, and known limitations.
- [ ] Keep `.agents/todo-8.md` updated throughout implementation.
- [ ] Mark a checkbox `[x]` only after its work is complete and the stated verification has actually been performed.
- [ ] Leave blocked, unexecuted, or human-review tasks unchecked and explain the blocker/evidence beside them.
- [ ] Run the established solution restore, Release warnings-as-errors build, and full solution test workflow through the existing CI gate.
- [ ] Confirm all existing and new test projects pass on the latest PR CI run.
- [ ] Confirm the post-merge Linux Release CI run passes before closing the milestone.
- [ ] Keep ordinary CI Release-only; do not add a permanent Debug job, Windows runner, package, project, or workflow solely for this milestone.
- [ ] Do not claim native Photino/WebView2 dialog, window-lifecycle, resize, or accessibility testing from headless tests or Linux CI.
- [ ] Record exact PR, merge commit, and passing CI run URLs in the handoff.
- [ ] Reconcile this checklist with the handoff and actual changed files; identify anything authored but not executed.
- [ ] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, visual-review status, artifact location, timing observations, and known limitations.

## Acceptance checklist

- [ ] All existing fruit, ceramic, mixed-scene, bowl-containment, cup-liquid, and tulip-in-vase prompts continue working unchanged.
- [ ] Both daisy-in-blue-vase and daisy-in-cream-vase prompts parse and generate through the existing desktop prompt-to-PNG path.
- [ ] Core represents the daisy as typed contents of one existing vase, not an unrelated scene entry or untyped generic object.
- [ ] The daisy has exactly one open bloom, one stem, and two leaves, with a fixed documented petal count.
- [ ] The bloom has clearly radial white petals and a distinct golden-yellow centre, and reads as a daisy at normal viewing size.
- [ ] The stem is derived from vase-local mouth geometry and appears to enter the vase naturally.
- [ ] The vase mouth/rim/body correctly occludes the hidden stem section; the bloom and leaves remain visible above the opening.
- [ ] Daisy geometry is deterministic for identical inputs and bounded across seeds, variation, and supported output sizes.
- [ ] Daisy random streams do not perturb tulip, vase, fruit, cup, or bowl output.
- [ ] The daisy adds no unintended tabletop shadow, vase-body repainting, exterior leakage, or canvas-edge collision.
- [ ] Automated Core/parser, renderer invariant, desktop integration, and existing regression tests pass.
- [ ] Generated full-resolution samples and a correctly mapped review sheet are available; visual review is honestly recorded as passed or pending.
- [ ] README and handoff accurately document supported prompts, architecture, evidence, exclusions, and limitations.
- [ ] The completed change passes the repository's Linux Release CI gate and post-merge run.
- [ ] The agent stops and reports implementation decisions, actual test/CI evidence, sample artifacts, visual observations, and known limitations.
- [ ] Do not continue to another flower species, plant family, container type, or relationship milestone without review.

## Explicitly out of scope

- Roses, sunflowers, lilies, orchids, wildflowers, or any flower species beyond the existing tulip and this one daisy.
- Multiple flowers, bouquets, multiple stems/blooms, arbitrary leaf counts, or general plant arrangements.
- Flower pots, soil, gardens, outdoor scenes, trees, shrubs, grass, branches, or general vegetation.
- Flowers in bowls/cups, liquids in vases, fruit in vases, or any additional containment relationship.
- Multiple vases, additional scene objects in a daisy-in-vase prompt, or arbitrary spatial instructions.
- Vase redesign, new ceramic colours, table/background redesign, or unrelated material changes.
- Animated growth, wind, physics, botanical simulation, 3D, GPU rendering, neural models, or AI prompt translation.
- A generalized botanical/material/relationship framework, scene graph, plugin architecture, or unrestricted natural-language parser.
- New packages, projects, frameworks, CI runners, permanent workflow jobs, or unrelated refactors.

## Working instruction

Implement this milestone directly in the existing repository, in small coherent steps, with tests as the system grows. Make reasonable engineering decisions without asking the user to design internal architecture. Do not stop after writing a plan or only documenting the work: complete the scoped implementation.

Preserve every existing generation path, especially the tulip-in-vase path. Use the existing vase mouth profile as the source of truth for stem attachment and draw order. Keep the daisy representation explicit and species-appropriate; do not force its radial bloom into tulip geometry or generalize the project into a botanical framework. If the current model exposes a genuine blocker, explain the evidence and choose the smallest compatible alternative rather than broadening scope. Keep this checklist accurate throughout, stop at the acceptance checkpoint, and provide the complete handoff.
