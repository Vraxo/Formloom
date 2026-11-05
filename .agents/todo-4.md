# Formloom — Phase 4: Ceramic Vessels and Mixed Still-Life Scenes

## Objective

Expand Formloom beyond fruit by introducing a small, coherent family of **ceramic vessels** and allowing them to appear in the same composed still-life as the existing fruit.

This milestone adds exactly three vessel types:

- **Bowl** — a broad, low vessel with a visible opening, inner cavity, rim and small foot.
- **Cup** — a smaller, taller drinking vessel with a visible opening/interior and a clearly open handle.
- **Vase** — a taller decorative vessel with a distinct base, body, shoulders, neck and mouth.

Give the vessels two explicit glaze palettes: **blue** and **cream**. The material should read as glazed ceramic, not fruit skin or painted plastic.

The existing apple, pear and orange remain unchanged in vocabulary and recognizable appearance. The current prompt-to-PNG path must produce single vessels and mixed scenes containing fruit and vessels. The new geometry, layout, rendering and prompt handling must be reusable within this deliberately small vocabulary.

**This is a 2D procedural still-life renderer.** Use layered contours, ellipses, gradients, masks and shadows to suggest convincing volume and openings. Do not introduce 3D meshes, a camera, physics, a general vector editor or a universal scene graph.

## Starting point

- Start from the latest `main`; inspect the actual `HEAD` and working tree before editing.
- Phase 3 handoff: `docs/composition-handoff.md`.
- Phase 3 checklist: `.agents/todo-3.md`.
- Phase 2 handoff: `docs/organic-forms-handoff.md`.
- Existing stack: .NET 10 / C# 14, `Formloom.Core`, `Formloom.Rendering.Skia`, `Formloom.Desktop`, `Formloom.Tests`, `Formloom.Desktop.Tests`, PhotinoX.Blazor, SkiaSharp CPU rendering, and the existing Linux Release CI workflow.
- Existing scene composition supports one to three fruit instances, and the original single-fruit API/rendering path has compatibility requirements.

## Project constraints

- [ ] Preserve the existing stack, target framework, package versions, project boundaries, analyzer policy, and CI workflow unless an evidence-based blocker makes a small change necessary.
- [ ] Keep generation offline, CPU-first, non-neural, seeded, deterministic, and inspectable.
- [ ] Keep Core independent of Skia, pixels, bitmaps, native resources and desktop/UI types.
- [ ] Reuse the existing prompt → Core scene construction → generation service → Skia renderer → PNG/save path.
- [ ] Preserve every supported single-fruit and fruit-group prompt, its subject ordering, deterministic behavior, metadata and save naming.
- [ ] Preserve the accepted apple, pear and orange profiles/materials. Do not rewrite them merely to make the new types fit a convenient abstraction.
- [ ] Keep the existing prompt, seed, variation, Generate, preview and Save PNG UI. Do not add manual object-positioning controls or a material editor.
- [ ] Add no downloaded assets, stock textures, fonts, network dependencies, image models, new projects or frameworks.
- [ ] Avoid a generic arbitrary-object scene graph, plugin registry, reflection-based type system, 3D engine, physics solver or speculative extensibility framework.
- [ ] Extend the README and handoff so they describe the actual current capabilities; do not leave the old apple-only description presented as the current product state.
- [ ] Keep this milestone limited to the three named ceramic forms and scenes containing at most three total objects.

## Exact scope and prompt vocabulary

### Existing fruit descriptors — retain exactly

- `a red apple`
- `a green apple`
- `a yellow apple`
- `a green pear`
- `a yellow pear`
- `an orange`

### New ceramic descriptors

Support exactly these six new descriptors:

- `a blue ceramic bowl`
- `a cream ceramic bowl`
- `a blue ceramic cup`
- `a cream ceramic cup`
- `a blue ceramic vase`
- `a cream ceramic vase`

Each complete prompt ends with the existing setting phrase: `on a wooden table`.

### Prompt examples to support

Single-object examples:

- `a blue ceramic bowl on a wooden table`
- `a cream ceramic cup on a wooden table`
- `a blue ceramic vase on a wooden table`

Mixed examples:

- `a red apple and a blue ceramic bowl on a wooden table`
- `a green pear and a cream ceramic cup on a wooden table`
- `an orange and a blue ceramic vase on a wooden table`
- `a red apple, a blue ceramic bowl, and an orange on a wooden table`
- `a cream ceramic cup, a green pear, and a blue ceramic vase on a wooden table`

Use the existing one-to-three item list syntax and normalization rules: conjunctions, optional comma/Oxford-comma form for three items, case normalization, whitespace normalization and terminal punctuation. Preserve prompt order in the Core scene description. The layout may independently assign positions and depth.

Do not add `beside`, `in`, `inside`, `on top of`, `behind`, left/right, plural forms, quantity words, extra adjectives, other colors, alternative table settings or arbitrary natural-language interpretation. The layout chooses a coherent arrangement; prompts identify the objects, not exact spatial relationships.

## Sequential implementation checklist

### 0. Inspect and record the real starting point

- [ ] Start from the latest `main`; record the exact starting commit and confirm the working tree is clean.
- [ ] Read the Phase 3 checklist and handoff, current README, scene/instance types, `SceneValidator`, `FruitComposition`, prompt parser, scene generator, fruit profile/material code, renderer passes, save metadata, artifact-export tests and desktop/component tests.
- [ ] Trace the existing one-fruit, fruit-pair and fruit-trio paths from prompt parsing through composition, rendering, PNG metadata and saving.
- [ ] Identify which current composition/rendering primitives can be reused for a vessel and which assumptions are fruit-specific.
- [ ] Identify how Phase 3 preserves prompt order separately from depth order, and retain that behavior for mixed scenes.
- [ ] Inspect the current test artifact glob/path and use the established `procedural-preview` artifact rather than inventing another workflow.
- [ ] Add a concise implementation note to the PR description naming the smallest Core, renderer, parser, desktop and test changes needed.
- [ ] Choose the smallest typed extension that supports only `Fruit` and `Ceramic` scene items; do not implement an open-ended object framework.

### 1. Add typed ceramic scene data and vessel profiles

- [ ] Add explicit Core types for ceramic kind and glaze color; use closed enums or equivalent typed values rather than arbitrary strings.
- [ ] Add a typed ceramic instance representation carrying kind, glaze, deterministic local seed and the placement/depth information needed by the existing scene coordinate system.
- [ ] Extend the scene model so fruit and ceramic instances can coexist while preserving existing fruit collection accessors and compatibility constructors used by callers/tests.
- [ ] Keep the heterogeneous scene representation closed and explicit. A small tagged projection over fruit/ceramic instances is acceptable for common sorting/validation; do not use `object`, reflection, string type names or an extensible registry.
- [ ] Preserve the original prompt order in the scene representation. Create a separate stable depth-sorted view for rendering, as Phase 3 does.
- [ ] Add a small immutable, normalized vessel-profile representation in Core, with enough data for the outer silhouette, opening/rim, interior/cavity, foot and optional handle attachment/bounds.
- [ ] Reuse low-level curve/vector utilities such as `CubicSegment` where they genuinely fit, but do not force vessel geometry into fruit-specific `OrganicContour` semantics.
- [ ] Keep profiles finite, normalized, bounded and independent of Skia types.
- [ ] Define a bowl profile with a broad mouth, shallow curved body, visible cavity/rim and proportionate foot.
- [ ] Define a cup profile with a readable drinking opening, interior, body taper and explicit handle geometry/attachment anchors.
- [ ] Define a vase profile with a recognizable base, broad lower body, shoulders, narrower neck and visible mouth.
- [ ] Ensure the three profiles have recognizably different silhouettes even when rendered as flat black shapes.
- [ ] Keep geometry separate from glaze/material appearance; blue and cream must not require different geometry implementations.
- [ ] Add Core validation for ceramic kind/color, profile bounds, handle/rim extents, finite transforms and valid total scene-item counts.
- [ ] Keep invalid/empty/excessive collections as explicit validation failures with actionable error messages.

### 2. Extend deterministic composition to mixed objects

- [ ] Extend the existing composition mechanism, or add the smallest typed extension, to place fruit and ceramic instances in one coordinate system.
- [ ] Keep the total scene-item limit at three, counting fruit and ceramic objects together.
- [ ] Preserve the existing exact one-fruit layout and avoid changing its random-stream consumption or accepted placement without a demonstrated regression fix.
- [ ] Preserve the existing one/two/three-fruit compositions as closely as practical.
- [ ] Add a centered single-vessel arrangement for bowl, cup and vase.
- [ ] Support two-object mixed compositions such as fruit + bowl, fruit + cup and fruit + vase.
- [ ] Support three-object scenes containing both fruit and ceramic objects.
- [ ] Account for the very different aspect ratios: low/wide bowl, compact handled cup, tall vase, and the existing fruit shapes.
- [ ] Include all visible geometry in layout bounds: cup handle, bowl rim, vase mouth and foot, and fruit stems/leaves/calyx.
- [ ] Keep complete subjects inside safe canvas margins for all supported output dimensions.
- [ ] Keep all objects grounded on the same tabletop plane with appropriate contact heights and scale-aware shadows.
- [ ] Maintain readable silhouette visibility; allow only restrained intentional overlap, and do not let a vessel handle, opening or fruit attachment disappear accidentally.
- [ ] Establish deterministic front-to-back depth ordering across both object categories.
- [ ] Use a separate scene-composition random stream from fruit geometry, ceramic profiles/materials and surface details.
- [ ] Derive stable per-instance local random streams so adding/reordering a different object does not unnecessarily perturb existing fruit or ceramic details.
- [ ] Make identical prompt + seed + variation produce identical placements and pixels on the same supported runtime.
- [ ] Let alternate seeds produce coherent alternative arrangements; variation must not change object count/type or collapse the composition.
- [ ] Prefer a small number of comprehensible layout strategies over a physics or optimization framework.
- [ ] Do not implement object containment, stacking, pouring, fruit inside a bowl, fruit inside a vase, or fruit placed on a vessel in this milestone.

### 3. Implement ceramic CPU rendering and shared scene passes

- [ ] Add ceramic rendering to the existing Skia renderer without creating a separate application/rendering pipeline.
- [ ] Draw the table/background once per scene, not once per object.
- [ ] Render all scene-item ground/cast shadows in a dedicated first pass before drawing any object bodies.
- [ ] Draw fruit and ceramic instances in a stable shared depth order so a foreground object correctly occludes a rear object.
- [ ] Keep each object's internal layer order coherent when drawing it as one depth-sorted item.
- [ ] Draw a cup handle behind its body, with a genuinely open center showing the scene behind it; draw the body over the handle attachment joins so they look connected.
- [ ] Draw bowl/vase/cup rear rims and interior surfaces before the outer body where appropriate, then draw the front rim/lip and restrained highlights in the correct foreground order.
- [ ] Make the bowl cavity visibly concave rather than a flat painted oval or closed disk.
- [ ] Make the cup read as a handled cup rather than a plain cylinder; keep the opening and inner wall visible.
- [ ] Make the vase read as an open vessel rather than a solid decorative blob; its mouth, neck, shoulders and base must remain legible.
- [ ] Use gradients, edge shading and highlights to give the ceramic coherent volume under the existing scene light direction.
- [ ] Implement a blue glaze palette with controlled highlights and shaded edges.
- [ ] Implement a warm cream glaze palette with controlled highlights and shaded edges.
- [ ] Make both palettes read as glazed ceramic, clearly distinct from the fruit's skin/materials and from flat matte paint.
- [ ] Keep surface variation restrained; do not add orange-like pores, noisy speckles or random decorative patterns to ceramic.
- [ ] Keep glaze, geometry and random detail streams separate.
- [ ] Scale rim thickness, handle thickness, highlights, interior shading and shadows with output dimensions/object transforms.
- [ ] Ensure the cup handle's opening and all vessel cavities behave correctly in normal rendering and complete-subject silhouette rendering.
- [ ] Extend silhouette rendering to include each vessel's complete exterior silhouette and handle, while keeping intentional openings transparent where appropriate and documenting this convention.
- [ ] Keep contact shadows attached to each object's actual base/contact region; cast shadows must agree with the existing light direction.
- [ ] Ensure overlapping bodies and handles are composited in the expected order without drawing a rear object's details over a foreground object.
- [ ] Validate that all temporary Skia resources are disposed per render/pass; do not introduce static native-resource caches.
- [ ] Preserve the current fruit-only rendering behavior and inspect any pixel/visual regressions before accepting them.
- [ ] Measure representative one-vessel and mixed three-object 1024 × 1024 render-plus-encode timings; report observed figures without inventing a hard benchmark guarantee from a single CI run.

### 4. Extend the strict prompt grammar, generation and save metadata

- [ ] Retain every currently supported single-fruit prompt and every supported fruit pair/trio prompt exactly.
- [ ] Parse all six new ceramic descriptors as typed kind/glaze combinations.
- [ ] Parse single ceramic prompts using the existing wooden-table setting phrase.
- [ ] Parse two- and three-item mixed fruit/ceramic lists with the existing conjunction/comma/Oxford-comma rules.
- [ ] Preserve prompt order and explicit object identity/color in the parsed scene.
- [ ] Reject unsupported ceramic colors, missing `ceramic` wording, unknown vessel types, extra adjectives, spatial prepositions, malformed lists and more than three total items.
- [ ] Reject any unsupported phrase as a whole; never partially consume a list, drop an item or silently fall back to fruit-only output.
- [ ] Preserve case, whitespace and terminal-punctuation normalization.
- [ ] Route all new prompts through the same generation service and renderer entry point.
- [ ] Preserve seed and variation snapshots, randomize behavior, background execution, duplicate-action guards, error display and previous-preview retention on failure.
- [ ] Preserve existing successful-image metadata when the prompt/seed controls are edited but not regenerated.
- [ ] Give single ceramic images safe, deterministic subject tokens and filenames.
- [ ] Preserve existing fruit-only single and `fruit-group` metadata/filenames wherever already established; do not rename them as an incidental consequence of mixed-scene support.
- [ ] Give ceramic-only groups and mixed fruit/ceramic groups distinct, explicit whitelisted metadata tokens (for example `ceramic-group` and `still-life-group`) rather than using raw prompt text.
- [ ] Update the filename whitelist and tests for all new tokens; reject untrusted raw prompt text as a filename source.
- [ ] Update the existing prompt help text with a concise ceramic or mixed-scene example; do not add new controls or panels.

### 5. Add automated Core and composition tests

- [ ] Test parsing of every existing single-fruit, pair and trio prompt after the grammar extension.
- [ ] Test all six ceramic descriptor mappings to the correct kind and glaze.
- [ ] Test single ceramic prompts and valid mixed two-/three-object prompts in different prompt orders.
- [ ] Test repeats where the grammar naturally permits them, or document a deliberate strict rejection without weakening existing fruit-list behavior.
- [ ] Test that parsed item count, kind, color and prompt order exactly match the input descriptors.
- [ ] Test rejection of unsupported colors, modifiers, object names, malformed separators, unsupported relationships and lists beyond three.
- [ ] Test vessel profiles for finiteness, normalization, continuity/closure, bounded dimensions and deterministic construction.
- [ ] Test that bowl, cup and vase profiles differ through semantic geometric invariants, not merely enum values.
- [ ] Test bowl mouth-to-height/width proportions and a nonzero cavity depth.
- [ ] Test cup handle bounds, attachment points and a genuine open center.
- [ ] Test vase base/body/neck proportions and a nonzero mouth opening.
- [ ] Test complete bounds include rims, handles, feet and other visible geometry.
- [ ] Test scene validation for empty/excessive total item counts, invalid typed values and non-finite/out-of-range transforms.
- [ ] Test single-vessel centered layout and mixed pair/trio placement across representative seeds and variations.
- [ ] Test that all complete subjects stay within margins, objects remain grounded and the layouts remain within defined spacing/overlap bounds.
- [ ] Test that a different seed can change layout while retaining the requested subject set.
- [ ] Test that adding/reordering a different scene item does not consume or shift an existing instance's local geometry/material/detail streams unnecessarily.
- [ ] Test that fruit-only prompt/layout compatibility remains intact, including repeated fruits and the original single-fruit compatibility path.

### 6. Add renderer and desktop integration tests

- [ ] Test valid PNG output for each vessel type in both glaze palettes at 1024 × 1024.
- [ ] Test representative smaller, larger and rectangular image sizes already supported by the renderer.
- [ ] Test identical ceramic and mixed-scene inputs produce identical decoded pixels on the same runtime.
- [ ] Test alternate seeds produce controlled differences without changing requested object identity/count.
- [ ] Test the bowl cavity, cup opening/handle and vase mouth are visibly represented by pixel/silhouette invariants that cannot pass vacuously.
- [ ] Test the cup handle hole remains open and the handle remains connected to the body at attachment joins.
- [ ] Test detail/material rendering remains clipped to each object's own geometry and cannot leak through foreground objects.
- [ ] Test complete-subject bounds and transparent canvas borders for each vessel and representative mixed scenes.
- [ ] Test vessel contact/cast shadow masks for grounding, direction and canvas-edge safety.
- [ ] Add targeted mixed-depth occlusion coverage for at least one fruit in front of a vessel and one vessel in front of a fruit.
- [ ] Test diagnostic renderer passes do not alter subsequent normal-render output.
- [ ] Retain all existing fruit, fruit-group, shadow, clipping, PNG, resource-ownership and repeatability tests.
- [ ] Extend the real desktop session → generation service → renderer → save-snapshot tests for single vessels and mixed scenes.
- [ ] Extend rendered-component tests to submit at least one single-vessel and one mixed-scene prompt.
- [ ] Test unsupported mixed prompts retain the last successful image and matching save metadata.
- [ ] Test all new filename tokens and ensure existing apple/pear/orange and fruit-group names remain unchanged.

### 7. Generate compact visual-review artifacts

- [ ] Extend the existing `procedural-preview` test artifact; do not introduce another CI workflow or artifact mechanism without necessity.
- [ ] Generate default-seed/default-variation single PNGs for bowl, cup and vase, including both glaze palettes where useful.
- [ ] Generate representative mixed pairs: apple + bowl, pear + cup and orange + vase.
- [ ] Generate at least one mixed trio containing both fruit and ceramic objects.
- [ ] Generate representative alternate-seed and low/high-variation cases for vessel and mixed-scene rendering.
- [ ] Generate complete-subject silhouettes for all three vessels and representative mixed scenes.
- [ ] Generate matching timing sidecars for representative vessel and mixed-scene cases if the existing exporter already supports them.
- [ ] Prefer one compact labeled review sheet/overview assembled with already available dependencies, plus a small number of individual full-resolution PNGs; do not add a package or project just to make a collage.
- [ ] Keep generated PNGs and review composites in ignored test-output/artifact directories; do not commit generated binaries as source assets.
- [ ] Ensure filenames identify object descriptors, seed and variation unambiguously.
- [ ] Inspect the actual rendered output where the working environment allows it. Do not mark visual review complete merely because a PNG was generated or uploaded.
- [ ] If the coding environment cannot access the actual generated pixels, leave the human visual-review gate explicitly pending and provide the artifact link/details needed for review; do not fabricate a visual pass.

### 8. Documentation, verification and handoff

- [ ] Update README's current-capability section to include fruit, ceramic vessels and mixed scenes; remove or clearly label stale apple-only statements as historical milestone notes.
- [ ] Document the exact supported single-object and mixed prompt vocabulary, list limit, glaze choices and unsupported spatial relationships.
- [ ] Update the README layout/project overview to reflect the actual current scene and rendering architecture without duplicating the entire handoff.
- [ ] Add `docs/ceramic-vessels-handoff.md` covering starting/ending commits, key design decisions, new Core types, geometry/rendering approach, grammar, metadata, tests, CI evidence, generated review artifacts, timing observations and known limits.
- [ ] Update this `.agents/todo-4.md` throughout implementation; mark a checkbox `[x]` only after the item is completed and its stated verification is performed.
- [ ] Run the established solution restore, Release warnings-as-errors build and full solution test workflow through the existing CI gate.
- [ ] Confirm all existing and new test projects pass in the latest PR and post-merge Linux Release CI runs.
- [ ] Do not add a permanent Debug job, Windows runner, package or workflow solely for this milestone.
- [ ] Do not claim native Photino/WebView2 dialog or OS-specific GUI testing from headless component tests or Linux CI.
- [ ] Record exact PR, merge commit and passing CI run URLs in the handoff.
- [ ] Reconcile the checklist with the handoff and actual changed files; identify anything authored but not executed.
- [ ] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, visual-review status, timing evidence, artifact location and known limitations.

## Acceptance checklist

- [ ] The original apple, pear and orange prompts, fruit pairs and fruit trios still work with their established appearance, metadata and save naming.
- [ ] Bowl, cup and vase each have a distinct, immediately recognizable silhouette and visible open-vessel geometry.
- [ ] Both blue and cream glaze palettes read as glazed ceramic and remain separate from geometry.
- [ ] Single vessels and mixed fruit/ceramic scenes generate through the existing desktop prompt-to-PNG path.
- [ ] One-, two- and three-object scenes use deterministic, bounded composition with correct depth order, complete bounds and coherent grounding/shadows.
- [ ] Scene model remains a small, explicit typed extension; no generic arbitrary scene graph or separate rendering pipeline was introduced.
- [ ] Core, renderer and desktop tests cover the new capability and all existing tests pass.
- [ ] Generated visual-review artifacts are available, and the handoff accurately distinguishes automated checks from actual pixel review.
- [ ] README, handoff, prompt help and safe save metadata describe the implemented capability accurately.
- [ ] The completed change passes the established Linux Release CI gate, with exact run evidence recorded.
- [ ] The agent stops. Do not begin another milestone or add a fourth object family before review.

## Explicitly out of scope

- Plates, saucers, mugs as a separate fourth type, teapots, pitchers, bottles, jars, baskets, cutlery, cloth, furniture or additional props.
- More than three total scene objects.
- Fruit inside bowls, fruit inside vessels, objects stacked on/inside one another, pouring, liquid, flowers in vases or other semantic containment/interaction.
- User-authored left/right/behind/in-front/inside positioning or a manual spatial editor.
- Additional fruit types, plants, flowers, food, animals, people, buildings or landscapes.
- Arbitrary ceramic shapes, a general material editor, unrestricted prompt parsing, synonym expansion or an AI prompt translator.
- 3D geometry, camera/perspective systems, physics, GPU rendering, neural models, downloaded textures or photorealism claims.
- Unrelated architecture refactors, broad renderer rewrites or changes to the accepted fruit profiles without a demonstrated need.

## Working instruction

Implement this milestone directly in the existing repository, starting from current `main`, in small coherent steps with tests as the system grows. Make reasonable internal engineering decisions without asking the user to design the architecture. Do not stop after writing a plan, updating documentation or creating placeholder types: complete the scoped implementation. Keep the task file current as work proceeds. If repository inspection reveals a genuine architectural blocker, document the concrete evidence and implement the smallest compatible alternative rather than expanding scope. Stop after the acceptance checklist and provide a concise, evidence-based completion report.
