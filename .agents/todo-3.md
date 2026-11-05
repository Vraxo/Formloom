# Formloom — Phase 3: Multi-Fruit Scene Composition

## Objective

Move Formloom from rendering one isolated fruit in a fixed still-life setting to arranging **two or three existing fruit instances together in one coherent image**.

This milestone is about composition, not adding more object types. The existing apple, pear, and orange should become reusable scene instances with independent transforms and seeded variation. A small, deterministic composition system should arrange them on the existing wooden table, with believable scale, spacing, depth order, grounding, and shadows.

The milestone is successful when the existing prompt-to-PNG pipeline can produce coherent single-fruit, two-fruit, and three-fruit still lifes using the same fruit construction and rendering path.

## Starting point

- Latest reviewed repository state: `main` (Phase 2 acceptance was closed in PR #21; inspect the actual current `HEAD` before making changes).
- Existing fruit subjects: red/green/yellow apple, green/yellow pear, and orange.
- Existing stack and boundaries: .NET 10 / C# 14, Core, Skia CPU renderer, PhotinoX.Blazor desktop, existing test projects and Linux Release CI.
- Phase 2 handoff: `docs/organic-forms-handoff.md`.
- Phase 2 checklist: `.agents/todo-2.md`.

## Project constraints

- [x] Preserve the existing stack, package versions, project boundaries, and CPU-first/offline/non-neural design.
- [x] Keep Core independent of Skia and desktop UI code.
- [x] Preserve seeded deterministic generation and independent named random streams.
- [x] Preserve every existing single-fruit prompt and its visual behavior as far as possible.
- [x] Keep the UI simple: existing prompt, seed, variation, Generate, preview, and Save PNG controls remain sufficient.
- [x] Do not add a new project, framework, package, network dependency, downloaded asset, font, texture, or image model without a demonstrated necessity and explicit approval.
- [x] Avoid speculative abstractions, universal scene-graph frameworks, plugin systems, and generic editors.
- [x] Do not start another milestone or add subject families after this checklist's acceptance checkpoint.

## Scope

### Included

- One, two, or three fruit instances in a single still-life scene.
- Existing apple, pear, and orange types only.
- A small typed scene-instance representation and a deterministic composition/layout step.
- Balanced arrangements with sensible relative scale, canvas margins, controlled proximity/overlap, depth order, table contact, and shadows.
- Strict support for simple fruit-list prompts, using the existing fruit descriptors and wooden-table setting.
- Seeded but controlled alternative arrangements; changing a seed should be able to change composition without changing which subjects were requested.

### Prompt grammar target

Retain every currently supported single-fruit prompt. Extend the grammar to support lists of two or three fruit descriptions, joined by `and`, with an optional comma/Oxford-comma list form for three. Examples:

- `a red apple and a green pear on a wooden table`
- `a green pear and an orange on a wooden table`
- `a red apple, a green pear, and an orange on a wooden table`
- `a yellow apple, a yellow pear, and an orange on a wooden table`

Use the existing explicit fruit/color vocabulary. Repeated fruit descriptions may be supported if they naturally fit the grammar, but do not add pluralization, quantity words, arbitrary adjectives, new colors, or positional prepositions in this milestone. The generator chooses a suitable group arrangement; the prompt does not claim to specify left/right/behind relationships.

### Explicitly out of scope

- New fruit or other subject types.
- Bowls, plates, baskets, cutlery, cloth, vases, furniture, or additional props.
- More than three fruit instances.
- User-authored left/right/behind/foreground positioning or a spatial editor.
- General-purpose scene graphs, scene scripting, unrestricted natural-language parsing, synonym expansion, or an AI prompt translator.
- New backgrounds, rooms, landscapes, camera systems, 3D, GPU rendering, neural models, or photorealism claims.
- UI panels or controls for manually positioning/scaling individual objects.

## Implementation status

Started at `36eaf70` with a clean working tree. Implementation is merged in PR #22 at `f49b2be`. [Post-merge Linux Release run 41622641615](https://github.com/Vraxo/Formloom/actions/runs/41622641615) passed restore, warnings-as-errors build, tests and artifact uploads. PR run 41622276993 also passed after fixing JSON argument formatting and restoring unsupported-prompt error wording. No local SDK was installed.

CI has generated the PNG/timing artifacts. The user supplied 20 group PNGs, which were visually reviewed (including white-composited silhouettes and attachment/shadow crops); **all required normal group cases and single-fruit baselines have now been supplied and visually reviewed; eight supplied group timing reports have been assessed**. The sandbox cannot retrieve GitHub's redirected artifact-storage hosts; user-supplied `procedural-preview` PNGs and timing JSON are needed. Source/test results do not replace visual review.

The post-review invariant follow-up (`CompositionInvariantTests`, friend-only renderer hooks, friend-assembly item) is merged in PR #24 at `0594577`, merge commit `9bc9ef1`. Its PR run [41671828179](https://github.com/Vraxo/Formloom/actions/runs/41671828179) and post-merge Linux Release run [41672188974](https://github.com/Vraxo/Formloom/actions/runs/41672188974) both report `Build` and `Test` steps `success` on the complete solution, so the new overlap-clipping, shadow-mask and diagnostic-repeatability tests did execute and pass. The working tree is clean and no evidence edits remain uncommitted; the final reconciliation below is recorded at `9bc9ef1`.

## Sequential implementation checklist

### 0. Inspect the current implementation

- [x] Start from the latest `main`; record the exact starting commit and confirm the working tree is clean.
- [x] Read the Phase 2 handoff, scene types, prompt parser, scene generator, fruit profiles, named random-stream implementation, renderer transforms, shadow/grounding code, desktop generation service, and relevant tests.
- [x] Trace the existing single-fruit generation path from prompt parsing through PNG encoding and save metadata.
- [x] Identify assumptions that enforce exactly one subject (for example, singular body properties, fixed center placement, one attachment origin, or subject-specific filename metadata).
- [x] Identify which current geometry, material, attachment, shading, and shadow helpers can already render an independently transformed fruit instance.
- [x] Write a brief implementation note in the PR description describing the smallest set of Core, renderer, parser, and test changes needed.
- [x] Do not redesign unrelated Phase 2 systems or rewrite accepted fruit profiles.

### 1. Introduce a small multi-instance scene representation

- [x] Extend the Core scene model to represent a collection of fruit instances rather than assuming one fruit body per scene.
- [x] Give each instance an explicit subject/appearance description and a normalized scene transform (position, scale, and depth/order as appropriate to the existing coordinate model).
- [x] Keep scene-level setting/background information separate from per-fruit instance information.
- [x] Preserve or deliberately migrate existing single-fruit public APIs with a narrow compatibility path where existing callers/tests require it.
- [x] Validate instance count, finite transform values, scale bounds, and normalized placement before rendering.
- [x] Ensure invalid or empty scene collections fail with useful validation results rather than producing malformed output.
- [x] Keep Core free of Skia, pixel, bitmap, or UI types.
- [x] Do not introduce a generic arbitrary-object scene graph; the scene model should remain focused on the existing fruit vocabulary.

### 2. Implement deterministic composition/layout

- [x] Add a small layout/composition component that accepts the scene's fruit instances and produces valid placements in the existing still-life coordinate system.
- [x] Support one-fruit placement that remains visually consistent with the current centered single-fruit composition.
- [x] Support a balanced two-fruit arrangement with distinct, readable silhouettes and natural spacing.
- [x] Support a balanced three-fruit cluster with clear visibility of all requested fruit types.
- [x] Account for each fruit's aspect ratio and attachment extent when estimating its occupied space.
- [x] Keep every complete subject (including stems/leaves/calyx) within safe canvas margins.
- [x] Prevent accidental subject clipping and unintended collisions with canvas edges.
- [x] Allow restrained, intentional overlap where it improves depth and still-life cohesion, without hiding the identity-defining silhouette or attachments of a fruit.
- [x] Establish a clear front-to-back draw order so overlap is stable and predictable.
- [x] Keep all fruit grounded on the same table plane; do not let scale/position changes create floating objects or detached contact shadows.
- [x] Use a scene/composition random stream separate from each fruit's geometry, material, and detail streams.
- [x] Derive stable per-instance random stream identities so adding a different fruit does not perturb an existing instance's shape/detail stream unnecessarily.
- [x] Make same seed + same prompt + same variation produce the same placements and pixels.
- [x] Allow a different seed to produce a meaningfully different but still coherent composition.
- [x] Keep variation bounded: it may influence layout subtly, but must not change requested subject count or make the arrangement collapse.
- [x] Ensure one-fruit scenes do not acquire unnecessary positional randomness merely because multi-fruit support exists.
- [x] Prefer a few understandable deterministic arrangement strategies over a generic physics engine, force solver, or optimization framework.

### 3. Render multiple transformed fruit instances

- [x] Refactor the renderer's top-level scene pass to draw the setting once and then draw all fruit instances in deterministic depth order.
- [x] Reuse the existing shared organic contour, fruit material, attachment, lighting, and clipped-detail code for each instance.
- [x] Apply each instance's transform consistently to body contours, stems, leaves, calyx, highlights, and surface details.
- [x] Ensure each instance's material and seeded surface details remain independent of the other instances.
- [x] Compute or adapt contact shadows for each fruit's actual position and scale.
- [x] Keep cast shadows coherent with the existing scene lighting direction and prevent shadow ordering from looking detached or contradictory.
- [x] Ensure overlapping subjects are composited in the expected depth order without attachments being accidentally drawn behind their own fruit body.
- [x] Keep the table grain and background scene-level and unchanged by the number of fruit instances.
- [x] Preserve the existing single-fruit renderer output as closely as practical; investigate any visible regression rather than accepting it as an incidental side effect.
- [x] Maintain sensible memory use and CPU performance when rendering three fruits at 1024 × 1024. CPU evidence assessed: five supplied trio reports took 175.39–291.43 ms for render plus PNG encoding. Source ownership audit confirms scoped native disposals, one raster surface and bounded 1–3 instance processing; no isolated peak-memory benchmark is claimed.
- [x] Keep resource ownership/disposal correct when a scene contains multiple instances.

### 4. Extend the strict prompt grammar

- [x] Retain all currently supported single-fruit prompts exactly.
- [x] Parse two fruit descriptions joined by `and` followed by the existing wooden-table setting.
- [x] Parse three fruit descriptions joined by commas and a final `and`.
- [x] Support the existing apple color variants, pear color variants, and orange in any valid list position.
- [x] Preserve existing case, whitespace, and terminal-punctuation normalization.
- [x] Preserve subject order from the prompt in the parsed scene description, even if the layout pass later chooses a separate depth order.
- [x] Reject unsupported colors, adjectives, objects, list lengths, conjunctions, and malformed combinations explicitly.
- [x] Do not silently drop a requested fruit or fall back to a single-fruit scene after a parse or layout error.
- [x] Keep error messages actionable and consistent with the existing Result/error-handling conventions.
- [x] Keep the desktop generation service, preview retention, seed handling, randomize action, and PNG-saving path shared by single- and multi-fruit scenes.
- [x] Update save filename metadata to describe a multi-fruit scene safely and predictably without relying on untrusted raw prompt text.
- [x] Ensure editing the prompt without generating does not relabel the image already displayed in the preview.

### 5. Add automated Core and composition coverage

- [x] Test parsing of every existing single-fruit prompt after the parser extension.
- [x] Test valid two-fruit prompts with different subject orders and color combinations.
- [x] Test valid three-fruit prompts, including Oxford-comma syntax.
- [x] Test unsupported list sizes, malformed separators, duplicate conjunctions, unsupported colors, and unknown adjectives/subjects.
- [x] Test that parsed scene instance count and subject/color order match the prompt.
- [x] Test scene validation for zero instances, excess instances, invalid transforms, and out-of-range scales.
- [x] Test that layout is deterministic for identical inputs.
- [x] Test that different seeds can change composition while retaining the same subject set.
- [x] Test that layout preserves all requested instances and does not generate non-finite or out-of-bounds transforms.
- [x] Test that complete-subject bounds include attachments and respect safe canvas margins.
- [x] Test that two- and three-fruit layouts maintain readable spacing/visibility within defined tolerances.
- [x] Test that per-instance random streams remain independent when another fruit is added or reordered.
- [x] Test that a single-fruit prompt still uses the intended single-fruit placement behavior.

### 6. Add renderer and desktop integration coverage

- [x] Test valid PNG generation for one-, two-, and three-fruit scenes at 1024 × 1024.
- [x] Test at representative smaller, larger, and rectangular output sizes already supported by the renderer.
- [x] Test identical multi-fruit inputs produce identical decoded pixels.
- [x] Test different seeds produce controlled pixel differences without losing or changing the requested subject set.
- [x] Test that the renderer draws all requested bodies and their corresponding attachments.
- [x] Test that contact/cast shadows are present and do not appear detached from their fruit. Contact darkness and actual crop review passed. `CastAndContactShadowMasksAreGroundedAndClearOfCanvasEdges` renders the real shadow pass through the friend-only `RenderShadowMask` hook and asserts contact alpha at each fruit's body bottom plus directional cast extent to the lighting side for pairs/trios across seeds 0/42/1337 and variation 0/1; it passed in PR run 41671828179 and post-merge run 41672188974.
- [x] Test that surface details remain clipped to the correct fruit body when objects overlap. `InstanceDetailCannotLeakOutsideItsOwnContourOrThroughForeground` deliberately overlaps orange/apple bodies, suppresses detail for exactly one depth, and requires unchanged pixels outside that instance's Core-derived body mask plus foreground-identical opaque overlap pixels; positive overlap/changed-pixel counts prevent vacuous success. Passed in the same runs. Existing outside-complete-mask invariant and visual overlap review also passed.
- [x] Test that no fruit, attachment, or required shadow is accidentally clipped by the canvas. Complete-subject border tests and visual review passed; the shadow-mask test additionally asserts zero alpha on every canvas-border pixel of the isolated real shadow pass for pairs/trios across seeds 0/42/1337 and variation 0/1. Passed in the same runs.
- [x] Test that existing apple opacity, fruit silhouettes, pear/orange materials, wood grain, and single-fruit regression coverage still pass.
- [x] Extend desktop generation/session/component tests to prove multi-fruit prompts use the existing real generation pipeline.
- [x] Test that failed multi-fruit prompts retain the previous successful preview and its metadata.
- [x] Test that PNG save filenames/metadata correctly describe the last successful single- or multi-fruit scene.
- [x] Do not claim native Photino dialog or window behavior is tested by headless component/service tests.

### 7. Generate and visually review actual scenes

- [x] Generate a single-fruit baseline for each existing subject family to check that Phase 2 quality has not regressed.
- [x] Generate at least three different two-fruit combinations, including apple + pear and pear + orange.
- [x] Generate at least two three-fruit combinations containing all three fruit types.
- [x] Generate alternate-seed variants for at least one two-fruit and one three-fruit prompt.
- [x] Inspect all samples at normal viewing size and inspect representative crops for attachment/shadow intersections. Ten supplied normal group renders and representative attachment/overlap/grounding crops inspected; the subsequent upload supplied the remaining normal pair and single baselines, all inspected.
- [x] Confirm that two-fruit scenes read as one composed still life rather than two unrelated centered cut-outs. Supplied apple/pear and orange/apple pairs pass; normal pear/orange pair subsequently supplied and reviewed.
- [x] Confirm that three-fruit scenes have a clear visual hierarchy and that every requested fruit remains recognizable. Verified in the supplied group samples; see visual findings in the handoff.
- [x] Confirm scale relationships feel plausible without making any fruit arbitrarily tiny or dominant. Verified in the supplied group samples; see visual findings in the handoff.
- [x] Confirm overlap and depth order are visually understandable. Verified in the supplied group samples; see visual findings in the handoff.
- [x] Confirm every fruit rests on the table and has a believable contact shadow. Supplied normal groups pass visual review; remaining normal pair/baseline samples were subsequently supplied and inspected.
- [x] Confirm the common lighting direction remains coherent across all subjects. Supplied normal groups pass visual review; remaining normal pair/baseline samples were subsequently supplied and inspected.
- [x] Confirm stems, leaves, and calyxes are not unintentionally hidden, detached, or clipped. Supplied normal groups pass visual review; remaining normal pair/baseline samples were subsequently supplied and inspected.
- [x] Confirm different seeds change composition in a controlled way rather than merely shifting the entire group off-center. Verified in the supplied group samples; see visual findings in the handoff.
- [x] Confirm low/high variation remains readable and does not cause collisions, clipping, or loss of subject identity. Verified in the supplied group samples; see visual findings in the handoff.
- [x] Include reviewable sample PNGs in the completion report or generated CI artifact; do not commit bulky generated output unless the repository already establishes that convention.

### 8. Documentation and verification

- [x] Update README with the exact supported single- and multi-fruit prompt syntax and current limitations.
- [x] Update the Formloom handoff with the scene-instance model, layout approach, ordering/overlap rules, named-stream strategy, tests, visual findings, and known limitations.
- [x] Update this checklist accurately as tasks are completed; leave unverified claims unchecked and document blockers.
- [x] Run the full existing restore/build/test workflow.
- [x] Ensure all projects build with warnings as errors and every test project passes.
- [x] Keep the established Linux Release CI as the routine automated gate; do not add a new permanent job/runner for this milestone.
- [x] Confirm the latest post-merge Release CI run passes before declaring completion.
- [x] Report the starting/ending commits, changed files, key design decisions, tests and CI evidence, sample PNGs, and known limitations. Recorded in [the Phase 3 completion report](../docs/composition-handoff.md#phase-3-completion-report): start `36eaf70`, implementation `f49b2be`, invariant follow-up `0594577`, ending `9bc9ef1`; 19 changed files listed; design decisions, test/CI evidence, reviewed sample images and limitations included.
- [x] Stop at the acceptance checkpoint below; do not begin additional object families or props. No new subject, prop, grammar or UI scope was started after this checkpoint.

## Acceptance checklist

- [x] Existing single-fruit prompts still generate successfully through the current desktop pipeline.
- [x] Two-fruit prompts generate two distinct, recognizable fruit instances in one coherent composition.
- [x] Three-fruit prompts generate all three requested instances in one coherent composition.
- [x] The renderer draws the setting once and reuses the same fruit construction/material pipeline for every instance.
- [x] Composition is deterministic for a given prompt, seed, and variation; different seeds can produce controlled alternative layouts.
- [x] Instance geometry/detail streams are independent enough that adding another fruit does not arbitrarily alter existing fruit details.
- [x] Every fruit remains inside the canvas, recognizably visible, correctly layered, and grounded on the table.
- [x] Contact/cast shadows, attachments, lighting, and surface-detail clipping remain coherent in multi-fruit scenes. Established by the reviewed normal pair/trio images and crops plus the merged invariant tests (contact/directional cast extent, transparent shadow borders, one-depth detail suppression against the Core body mask, opaque-overlap foreground equality).
- [x] Strict parsing rejects unsupported or malformed requests rather than silently omitting subjects.
- [x] Automated Core, renderer, desktop integration, and existing regression tests pass. The merged implementation passed in PR #22/post-merge run 41622641615, and the invariant follow-up passed in PR run 41671828179 and post-merge run 41672188974 (`Test` step `success` for the whole solution, so existing Phase 2/3 regression coverage remained green).
- [x] Actual single-, two-, and three-fruit sample images have been visually reviewed and supplied as artifacts or report attachments. Final batch supplied the remaining normal pear/orange pair and current single baselines; findings recorded in the handoff.
- [x] README and handoff accurately record the implemented prompt grammar, composition behavior, evidence, and limitations.
- [x] The completed change passes the repository's Release CI gate. PR #22/#23 gates passed, and the invariant follow-up passed both its PR run 41671828179 and the post-merge run 41672188974 at `9bc9ef1`. This documentation-only reconciliation is the only change after that green commit; it alters no source, test or renderer code.
- [x] The agent stops and reports at this milestone; no new props or subject family is started.

## Working instruction

Implement this milestone directly in the existing repository, in small coherent steps, with tests as the system grows. Make reasonable engineering decisions without asking the user to design internal architecture. Complete the scoped implementation rather than stopping after a plan. If inspection reveals that a requirement would force a disproportionate redesign, document the evidence and choose the smallest alternative that still meets the acceptance criteria. Do not expand the grammar or object vocabulary beyond this scope. Keep this checklist updated in the repository as work progresses, and stop at the acceptance checkpoint.


## Final coverage reconciliation follow-up (executed; CI green)

Existing passing Core/service/component/renderer tests, the PR #22 implementation note, the supplied image reviews and eight timing reports establish the previously unchecked implementation tasks. The native-resource audit found local `using` ownership for surface/snapshot/encoding, paints, shaders, mask filters, paths and builders, with at most three immutable instances and no retained renderer cache. This is source evidence plus successful prior renders, not a leak-stress/peak-RSS benchmark.

New `CompositionInvariantTests` deliberately overlap two fruit bodies, toggle detail for one depth at a time, compare actual overlapping foreground pixels to the independently rendered foreground, and inspect the isolated real contact/cast-shadow pass across seeds/variation extremes for contact, directional extent and transparent borders. Internal friend-only renderer hooks reuse the exact draw code; no public render option, UI control, package or new review exports are added. Normal rendering is unchanged: `DiagnosticPassesDoNotChangeSubsequentNormalRendering` re-renders after both diagnostic passes and requires byte-identical PNG output, and it passed together with the rest of the suite. The follow-up is merged in PR #24 at `0594577`; PR run 41671828179 and post-merge run 41672188974 both completed the `Build` and `Test` steps successfully, so all new-test boxes above are executed rather than authored-only. No extra manual PNG uploads were required for this test-focused change.
