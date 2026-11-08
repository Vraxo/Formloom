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

> **Progress — group 1** (typed daisy model, normalized geometry, grammar, generation, validation,
> Skia draw path, first Core + renderer test pass): implemented and exercised by the new
> `VaseDaisyTests` / `VaseDaisyRenderingTests`. The daisy is a second typed vase content
> (`VaseDaisy`), not a widened tulip: `FlowerKind.Daisy` plus `DaisyAppearance.WhiteWithGoldenDisk`,
> `DaisyPlant/Bloom/Petal/Disk/Leaf` geometry from `DaisyGeometry`, `DaisyMaterial` in the renderer,
> and a shared `VaseMouth`/`VaseStem` path so both species enter a vase identically.
>
> **Progress — group 3** closes the loop. The desktop side is covered by
> `VaseDaisyPipelineTests` (both prompts through `GenerationService` → `DesktopSession` → save
> metadata, rejection that keeps the previous preview and its seed/variation, three distinct images
> for one vase, and both tokens whitelisted) and by the two daisy prompts added to the component
> theory. Two more renderer properties landed: every one of the twelve petals has ink in its own
> sector around the disk, and the head never reaches the canvas edge at either variation extreme.
> `README` has a Phase 8 section, `docs/daisy-vase-handoff.md` exists, and
> `docs/vase-flower-handoff.md` now carries a [Corrections and provenance] section replacing the
> stale "Authored but not executed" claims, with the pre-fix sheet and the post-fix stem artifact
> distinguished and the stem-end measurement recorded against the tests that now guard it.
>
> Group 3 also found and fixed a real defect that the daisy merely exposed: the shared
> mouth-continuation pass reset the matrix to a fixed `Scale(1024, 1024)`, so on every canvas that
> was not the 1024 reference the stem's continuation was painted at reference coordinates — a
> stray stub and an empty mouth. The per-size property test is what caught it, and it now asserts
> both halves at 64 × 64 through 4096 × 1024.
>
> **Final close-out:** all checklist and acceptance boxes are complete. The timing sidecars were
> supplied and read, visual review passed on the fixed-build sheets, and the final post-merge Linux
> Release run [42452733510](https://github.com/Vraxo/Formloom/actions/runs/42452733510) is green on
> merge `c441a9a` (PR #51). See the handoff's timing, visual-review and verification sections.

### 0. Inspect and record the actual starting point

- [x] Start from latest `main`; record the exact HEAD commit and confirm the working tree is clean.
- [x] Read `.agents/todo-7.md` and `docs/vase-flower-handoff.md` in full.
- [x] Reconcile the Phase 7 handoff's stale `Authored but not executed` statements with the later post-fix artifact and visual review: verify the exact evidence, remove or rewrite contradictory claims, and distinguish the original pre-fix sheet from the later stem-continuation fix artifact. Do not overstate which full-resolution samples were inspected.
- [x] Inspect the current `VaseFlower`, `FlowerKind`, `TulipColor`, `TulipPlant`, `TulipGeometry`, and `TulipMaterial` implementations.
- [x] Trace a tulip prompt end-to-end through parsing, Core scene construction, validation, renderer, generation service, desktop session, preview metadata, and PNG save.
- [x] Inspect the exact draw order for flower clipping, vase cavity, far rim, near lip, vase contour, and stem continuation.
- [x] Inspect the current flower bounds, attachment invariants, random-stream derivation, silhouette diagnostics, detail-omission behavior, and procedural-preview exporter.
- [x] Inspect current Core and renderer test conventions, including non-vacuous pixel assertions.
- [x] Identify the smallest explicit model extension that can represent both tulip and daisy without weakening validation or breaking the tulip path.
- [x] Record the intended files and design choice in the PR description before implementation; do not write a speculative architecture document.

### 1. Extend the typed flower-in-vase model narrowly

- [x] Add daisy as an explicit supported flower kind/value, or use an equally narrow representation that fits the actual current model.
- [x] Represent the daisy's fixed white-petal/golden-centre appearance explicitly; do not reuse `TulipColor` to imply daisy colour.
- [x] Represent daisy geometry with a dedicated typed model (for example, a daisy plant/bloom/petal/disk representation) rather than pretending it is a `TulipPlant`.
- [x] Keep the vase instance referenced by the arrangement identical to the vase entry already held by the scene; do not duplicate transform, seed, glaze, profile, or depth.
- [x] Keep the flower out of `SceneEntry`; it remains typed vase content, not an independently positioned neighboring object.
- [x] Preserve the existing tulip representation and validation semantics wherever possible.
- [x] If the current `VaseFlower` shape must change, use an explicit closed representation with exhaustive validation; avoid an untyped object payload or string-based dispatch.
- [x] Keep flower species and appearance values independent of Skia paints, shaders, colours, paths, and native rendering resources.
- [x] Keep vase-flower contents mutually exclusive with `BowlContainment` and `CupContents`.
- [x] Validate exactly one supported vase and exactly one valid flower arrangement.
- [x] Reject undefined enum values, missing geometry, invalid petal/disk counts, non-finite coordinates, non-positive dimensions, and contradictory contents.
- [x] Keep ordinary empty vase scenes and all existing tulip scenes unchanged.

### 2. Construct a recognizable daisy in vase-local normalized coordinates

- [x] Derive the stem base/attachment from the existing vase profile's mouth centre, usable opening, and rim thickness.
- [x] Ensure the stem enters safely through the usable vase opening and does not emerge from the exterior wall or float beside the rim.
- [x] Define all daisy geometry in normalized scene/vase-local coordinates; do not hard-code 1024 × 1024 pixel positions.
- [x] Build one slender, gently curved green stem with restrained natural bend.
- [x] Build exactly two green leaves attached at different points on the stem.
- [x] Make leaves elongated and lance-shaped, with tapered tips and restrained centre veins; reuse only genuinely shared leaf helpers if they fit both species cleanly.
- [x] Build one open daisy bloom with a clearly radial, approximately symmetric arrangement around a distinct central disk.
- [x] Use a fixed, modest petal count (choose and document a count appropriate to the desired silhouette); keep the count constant across seeds and variation.
- [x] Give each petal a tapered attachment near the disk and a rounded or softly notched outer end; avoid generic identical oval spokes.
- [x] Arrange petals with slight, bounded overlap and small authored differences in length, width, angle, and curvature so the bloom feels organic rather than like a flat gear.
- [x] Ensure the petals visibly radiate from behind/around the central disk, with no gaps that make them look detached.
- [x] Build a distinct golden-yellow central disk with a gently domed or softly textured appearance; do not use a flat yellow circle with no volume cues.
- [x] Keep the disk visually centred over the petal roots and attached to the bloom; no floating gap or oversized button-like centre.
- [x] Make the flower read immediately as a daisy at normal viewing size, not a sunflower, generic star, gear, pinwheel, or tulip. Reviewed at full size and cell by cell on the fixed-build daisy and tulip sheets (run `42450438121`); the handoff records the pass.
- [x] Keep the bloom orientation and stem transition botanically plausible for a simple illustrated still life.
- [x] Keep the daisy proportionate to the existing vase; it must be clearly visible without overwhelming the vessel or canvas.
- [x] Keep the entire flower inside safe canvas margins across supported sizes, seeds, and variation values.
- [x] Use bounded deterministic variation for petal contour, rotation, stem bend, and leaf shape; variation must never change species identity, petal count, leaf count, or colour.
- [x] Use stable named random streams for daisy stem, leaves, petals, and central disk if randomness is useful.
- [x] Keep daisy streams independent of tulip geometry, vase geometry/glaze, and all fruit/cup/bowl streams.
- [x] Prefer an authored daisy profile with restrained seeded offsets over unconstrained radial noise or a general flower grammar.
- [x] Test normalized bounds, radial arrangement, disk centring, petal count, and vase attachment before rendering.

### 3. Render the daisy with correct materials and vase occlusion

- [x] Extend the existing CPU Skia renderer and retain the current renderer entry point and PNG encoder.
- [x] Draw the daisy behind the vase's foreground mouth/rim and body so the stem is naturally hidden where it enters the vase.
- [x] Keep the bloom and visible leaves above the vase opening; do not let the vase body cover the bloom.
- [x] Preserve the existing vase body, cavity, far rim, near lip, contour, foot, glaze, highlights, and shadows.
- [x] Render white petals with subtle warm/cool shading and restrained edge separation; avoid pure flat white shapes that merge into highlights.
- [x] Render the central disk in golden yellow with enough tonal variation to read as a distinct, gently domed centre.
- [x] Keep stem and leaves green, with consistent lighting and restrained gradients.
- [x] Clip all flower pixels to the expected flower region and vase-mouth overlap; prevent leakage onto the vase exterior, table, or background.
- [x] Preserve the existing stem-continuation mask and ensure the daisy stem enters the vase with the same continuous visual treatment as the tulip.
- [x] Keep the daisy's petal/disk rendering separate from the tulip petal material and bloom drawing; share only low-level helpers that are demonstrably shape-agnostic.
- [x] Ensure the flower does not add a tabletop cast shadow or alter the vase's existing shadow mask unless the handoff explicitly justifies a scoped change.
- [x] Preserve silhouette and detail-omission diagnostic semantics, extending them only as needed for daisy-specific geometry.
- [x] Ensure diagnostic rendering cannot contaminate a subsequent normal render.
- [x] Keep rendering CPU-friendly at 1024 × 1024 and representative supported sizes. Observed 134.228 ms for the canonical 1024² daisy and 21.635–25.2857 ms per 320 px sheet cell on the CI runner. These are single-sample observations, not a repeated benchmark or universal guarantee.
- [x] Preserve deterministic pixel output for identical inputs on the same supported runtime.

### 4. Extend the strict prompt, generation, and save path

- [x] Add exactly `a white daisy in a blue ceramic vase on a wooden table`.
- [x] Add exactly `a white daisy in a cream ceramic vase on a wooden table`.
- [x] Preserve existing case, whitespace, and terminal-punctuation normalization.
- [x] Reject all other daisy colours, flower species, quantities, vase/container combinations, extra objects, and unsupported relationship wording.
- [x] Ensure invalid daisy prompts cannot silently fall back to an empty vase or a tulip.
- [x] Route both daisy prompts through the same generation service, renderer, desktop session, preview, and PNG save path as existing scenes.
- [x] Add two deterministic subject tokens and safe whitelisted PNG filenames for the two daisy/vase combinations.
- [x] Preserve every existing tulip token/filename and every earlier subject token/filename unchanged.
- [x] Preserve last-successful-image metadata when prompt/seed/variation controls are edited without regenerating.
- [x] Do not add desktop controls, panels, or a flower editor; existing prompt input is sufficient.
- [x] Update concise prompt-help text with one daisy-in-vase example without listing an unbounded vocabulary.

### 5. Add Core, parser, and validation tests

- [x] Test both supported daisy/vase-glaze prompt combinations.
- [x] Test case normalization, repeated whitespace, terminal punctuation, and exact-grammar behavior.
- [x] Test that parsed daisy kind, fixed appearance, and vase kind/glaze match the prompt.
- [x] Test that the daisy is represented as typed vase content, not an independent `SceneEntry` or generic object.
- [x] Test that daisy contents are mutually exclusive with bowl containment and cup contents.
- [x] Test validation for exactly one supported vase and exactly one valid daisy arrangement.
- [x] Test rejection of missing vase, non-vase container, duplicate vase, extra independent entries, undefined kind/value, and contradictory relationships.
- [x] Test rejection of unsupported flower colours/species, reversed wording, alternative relationship phrases, plural/quantity forms, extra adjectives, malformed separators, and missing table phrase.
- [x] Test that invalid daisy prompts are rejected as a whole and cannot fall back to an empty vase or tulip.
- [x] Test stem attachment against the vase mouth's usable opening bounds.
- [x] Test that all stem/leaf/petal/disk geometry is finite, positive where applicable, normalized, and within canvas safety margins.
- [x] Test that exactly two leaves, one bloom, and the documented fixed petal count are represented across seeds and variations.
- [x] Test radial petal placement and central-disk centring with geometric assertions, not only enum comparisons.
- [x] Test deterministic geometry for identical inputs and bounded variation across seeds and variation extremes.
- [x] Test that changing the seed can produce controlled shape differences without changing flower identity or petal/leaf counts.
- [x] Test daisy stream independence from tulip, vase glaze, and existing fruit/cup/bowl streams.
- [x] Test that changing vase glaze does not change daisy geometry.
- [x] Retain all existing Core/parser/scene-validation/organic-form/containment/cup-liquid/tulip tests.

### 6. Add renderer pixel/invariant and desktop integration tests

- [x] Test valid PNG output for both daisy/vase combinations at 1024 × 1024.
- [x] Test representative smaller, larger, square, and rectangular output dimensions already supported by the renderer.
- [x] Test identical daisy inputs produce identical PNG bytes/pixels on the same supported runtime.
- [x] Test alternate seeds produce controlled differences while preserving daisy identity and vase glaze.
- [x] Add non-vacuous pixel invariants proving that white petals, yellow disk, both leaves, and stem are actually rendered.
- [x] Assert the bloom and central disk are above the vase mouth and are not clipped by the vase.
- [x] Assert the stem visibly reaches and enters the vase opening without detachment or exterior-wall leakage.
- [x] Assert the petals remain connected visually around the disk and do not collapse into a star/gear-like silhouette at supported sizes.
- [x] Compare a daisy-in-vase render against the same empty-vase render and prove changes are limited to the flower region and intentional mouth-overlap region.
- [x] Assert vase ceramic pixels outside the intended stem/rim overlap remain identical to the equivalent empty-vase render where the existing convention requires it.
- [x] Assert the daisy adds no unintended tabletop shadow and does not change the vase's existing shadow mask.
- [x] Assert white petal pixels and yellow disk pixels are distinguishable from each other and from vase highlights.
- [x] Assert flower pixels do not leak onto the table/background outside expected flower bounds.
- [x] Assert the flower remains inside canvas bounds at all supported output dimensions and variation 0 and 1.
- [x] Test silhouette and detail-omission diagnostics according to their documented behavior.
- [x] Test diagnostic rendering followed by normal rendering produces the same normal-render bytes as a fresh render.
- [x] Retain all existing fruit, ceramic, mixed-scene, bowl-containment, cup-liquid, tulip, silhouette, shadow, clipping, PNG, resource-ownership, and determinism tests.
- [x] Extend generation-service/desktop-session tests to submit both daisy prompts through the real prompt-to-PNG path.
- [x] Extend rendered-component tests to verify successful generation metadata for both daisy prompts.
- [x] Test that an unsupported daisy prompt retains the previous successful preview and matching save metadata.
- [x] Test both new subject tokens/filenames and confirm every existing filename remains unchanged.

### 7. Generate and review visual/performance artifacts

- [x] Extend the existing `procedural-preview` artifact; do not add a second workflow or artifact pipeline without a demonstrated need.
- [x] Generate default-seed/default-variation full-size PNGs for both daisy/vase combinations.
- [x] Generate at least one alternate-seed sample for each vase glaze.
- [x] Generate low- and high-variation examples for a representative daisy/vase combination.
- [x] Generate flower/vase silhouette diagnostics for representative cases and verify the documented convention.
- [x] Generate a compact review sheet and manifest identifying each cell's prompt, vase glaze, seed, variation, filename, and dimensions.
- [x] Include individual full-resolution 1024 × 1024 PNGs alongside the review sheet.
- [x] Keep generated PNGs and review composites in ignored test-output/artifact directories; do not commit generated binaries as source assets.
- [x] Generate timing sidecars using the existing exporter conventions.
- [x] Measure render-plus-PNG-encode time at 1024 × 1024 for daisy-in-vase scenes and compare with equivalent empty-vase and tulip-in-vase renders on the same runner/runtime.
- [x] Report timing observations as measurements from that environment, not universal performance claims. Read back from the run `42450438121` sidecars: daisy 134.228 ms, empty vase 130.7872 ms (+3.44 ms), tulip in the same glaze 130.0086 ms (+4.22 ms) at 1024²; the 320 px sheet's eight cells 21.64–25.29 ms each. Recorded as single samples, with the tulip's own two sidecars disagreeing by ~94 ms as the stated reason not to treat them as a cost model.
- [x] Inspect actual generated pixels where the environment permits; generating or uploading a PNG alone is not visual review.
- [x] Review that the flower reads immediately as a daisy without a label.
- [x] Review that the bloom has a clear radial petal arrangement and a distinct golden centre, not a sunflower, gear, star, pinwheel, or tulip.
- [x] Review that the stem enters the vase naturally and is hidden at the mouth/body in the correct places.
- [x] Review that exactly two leaves are visible, attached, balanced, and not awkwardly intersecting the bloom.
- [x] Review both blue and cream vase combinations and confirm the white/yellow flower remains legible against each. Reviewed on `formloom-vasedaisy-sheet-2.png` and `formloom-vaseflower-sheet-3.png` from the merged build (run `42450438121`): white petals and golden disk read against both glazes, and the warm brown cream interior keeps the stem and blades legible in the mouth where the blue vases use the navy cavity.
- [x] Review alternate seeds and variation extremes for broken petals, uneven gaps, malformed leaves, excessive bends, clipping, or canvas-edge collisions. Seeds 1337 and 20261105 at variation 0 and 1 in both glazes: twelve petals, no broken petal, both leaves on opposite sides, no clipping, ~8 px margin to the cell edge.
- [x] Review legibility at 320 px sheet cells: every cell reads immediately as a daisy or a tulip, and the golden disk and petal notches survive at that size. Measured 12–35 green px inside the daisy mouths and 21–46 in the tulips, with zero green ink on the lip or body below the mouth in all sixteen cells.
- [x] Review the group sheet for the stem disappearing at the mouth: every 320 px cell lost it. The cause was a fixed 1024 scale in the mouth-continuation pass; it now scales by the scene's size, and 320 px is covered by the size matrix, by a cell-by-cell scan of the composited sheet, and by the same check for the tulip.
- [x] Review vase rim, mouth, body, glaze, foot, and tabletop grounding for regressions.
- [x] Review that flower materials fit the existing illustrated still-life lighting and are not flat, neon, plastic, or excessively noisy.
- [x] If actual generated pixels cannot be inspected, leave the human visual-review gate pending and provide exact artifact names, sheet dimensions/order, and manifest details; do not fabricate a visual pass.
- [x] Record cosmetic observations separately from defects and do not expand scope to unrelated appearance preferences.

### 8. Documentation, verification, and handoff

- [x] Update the README current-capability section to mention one white daisy in a blue or cream ceramic vase.
- [x] Document the exact two supported daisy prompts and their strict grammar.
- [x] Document that the daisy contains one bloom, one stem, exactly two leaves, white petals, and a golden-yellow centre.
- [x] Document that tulips remain red or yellow and that daisy colours/species/quantities beyond this milestone remain unsupported.
- [x] Document that empty vases, tulip-in-vase scenes, fruit/ceramic groups, bowl containment, and filled coffee/tea cups remain supported.
- [x] Update the architecture overview only as much as needed to explain the explicit second flower representation and its draw order.
- [x] Add `docs/daisy-vase-handoff.md` with starting/ending commits, design decisions, typed Core representation, normalized geometry, materials, vase attachment/occlusion, grammar, subject tokens, tests, CI evidence, artifact names, timing observations, visual-review status, and known limitations.
- [x] Keep `.agents/todo-8.md` updated throughout implementation.
- [x] Mark a checkbox `[x]` only after its work is complete and the stated verification has actually been performed.
- [x] Leave blocked, unexecuted, or human-review tasks unchecked and explain the blocker/evidence beside them.
- [x] Run the established solution restore, Release warnings-as-errors build, and full solution test workflow through the existing CI gate.
- [x] Confirm all existing and new test projects pass on the latest PR CI run.
- [x] Confirm the post-merge Linux Release CI run passes before closing the milestone: [run 42452083956](https://github.com/Vraxo/Formloom/actions/runs/42452083956), green on merge `6c48a7a`.
- [x] Keep ordinary CI Release-only; do not add a permanent Debug job, Windows runner, package, project, or workflow solely for this milestone.
- [x] Do not claim native Photino/WebView2 dialog, window-lifecycle, resize, or accessibility testing from headless tests or Linux CI.
- [x] Record exact PR, merge commit, and passing CI run URLs in the handoff.
- [x] Reconcile this checklist with the handoff and actual changed files; identify anything authored but not executed.
- [x] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, visual-review status, artifact location, timing observations, and known limitations; handoff and final close-out report provide this summary.

## Acceptance checklist

- [x] All existing fruit, ceramic, mixed-scene, bowl-containment, cup-liquid, and tulip-in-vase prompts continue working unchanged.
- [x] Both daisy-in-blue-vase and daisy-in-cream-vase prompts parse and generate through the existing desktop prompt-to-PNG path.
- [x] Core represents the daisy as typed contents of one existing vase, not an unrelated scene entry or untyped generic object.
- [x] The daisy has exactly one open bloom, one stem, and two leaves, with a fixed documented petal count.
- [x] The bloom has clearly radial white petals and a distinct golden-yellow centre, and reads as a daisy at normal viewing size.
- [x] The stem is derived from vase-local mouth geometry and appears to enter the vase naturally.
- [x] The vase mouth/rim/body correctly occludes the hidden stem section; the bloom and leaves remain visible above the opening.
- [x] Daisy geometry is deterministic for identical inputs and bounded across seeds, variation, and supported output sizes.
- [x] Daisy random streams do not perturb tulip, vase, fruit, cup, or bowl output.
- [x] The daisy adds no unintended tabletop shadow, vase-body repainting, exterior leakage, or canvas-edge collision.
- [x] Automated Core/parser, renderer invariant, desktop integration, and existing regression tests pass.
- [x] Generated full-resolution samples and a correctly mapped review sheet are available; visual review is honestly recorded as passed or pending. Recorded as **passed** in [`docs/daisy-vase-handoff.md`](../docs/daisy-vase-handoff.md) after the two defects found in review were fixed and re-reviewed.
- [x] README and handoff accurately document supported prompts, architecture, evidence, exclusions, and limitations.
- [x] The completed change passes the repository's Linux Release CI gate and post-merge run.
- [x] The agent stops and reports implementation decisions, actual test/CI evidence, sample artifacts, visual observations, and known limitations.
- [x] Do not continue to another flower species, plant family, container type, or relationship milestone without review.

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
