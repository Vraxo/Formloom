# Formloom — Phase 2: Organic Forms

## Objective

Expand Formloom from a single apple subject into a small, coherent fruit family by introducing a reusable organic-form construction system.

The milestone is successful when **apple, pear, and orange** can be generated through the existing prompt → scene → CPU renderer → PNG pipeline, while sharing genuinely reusable geometry/rendering infrastructure. A pear must not simply be a stretched apple, and an orange must not simply be a circle with orange paint and random dots.

This is the next vertical slice, not a general-purpose shape editor or a broad expansion of the prompt language.

## Project constraints

- Preserve the existing stack and project boundaries: .NET 10 / C# 14, Core, Skia renderer, PhotinoX.Blazor desktop, and the existing test projects.
- Keep generation CPU-first, offline, non-neural, seeded, deterministic, and fully inspectable.
- Do not add machine learning, downloaded artwork, stock textures, external image assets, network calls, GPU dependencies, or new projects/frameworks.
- Keep Core independent of Skia and the desktop UI.
- Keep generation logic out of Razor components. The UI should continue to pass a prompt and settings through the existing service pipeline.
- Preserve the existing apple prompt behavior, seed/variation behavior, save behavior, and all current tests.
- Do not broaden the language into an unrestricted natural-language parser. Unsupported prompts should continue to fail explicitly.
- Follow the repository's established code style and test conventions. Avoid speculative abstractions and framework-building for hypothetical future objects.

## Scope

### Subjects

Retain the current apple variants and add:

- A green pear.
- A yellow pear.
- An orange.

Use the same wooden-table still-life setting and single-subject composition. The new subjects should feel like members of the same visual world as the existing apple, not separate demo renderers.

### Prompt examples

Retain all existing supported apple prompts, including:

- `a red apple on a wooden table`
- `a green apple on a wooden table`
- `a yellow apple on a wooden table`

Add these explicit prompts:

- `a green pear on a wooden table`
- `a yellow pear on a wooden table`
- `an orange on a wooden table`

Continue tolerating the existing case, whitespace, and terminal-punctuation variations. Do not silently interpret arbitrary extra adjectives or unsupported colors. Document the exact supported prompt forms in the README.

## Implementation status

The scoped implementation is authored; see [inspection/implementation notes](../docs/organic-forms-handoff.md) for files, reuse decisions, compatibility boundaries and verification limitations. No local .NET SDK is installed; Linux Release CI must execute the new tests. Sample export code is authored, but actual PNG generation/visual review and latest CI are **not yet performed**. Their checkboxes remain open. No generated binary artifacts are committed.

## Sequential implementation checklist

**Checklist rules for the agent**

- [ ] Work through the sections in order; do not skip ahead to implementation before inspecting the existing code.
- [ ] Mark a checkbox `[x]` only after the task is actually completed and verified.
- [ ] Keep this file updated in the repository as work progresses, so the user can inspect the remaining work at any point.
- [ ] Do not mark a test, visual review, documentation update, or CI check complete merely because it was planned or authored; it must actually be performed.
- [ ] If a task is blocked, leave it unchecked and record the blocker and evidence next to it.

### 0. Inspect the current implementation

- [x] Start from the latest `main` and confirm the working tree is clean.
- [x] Read the current scene model, prompt grammar, scene construction, apple geometry, material/shading code, random-stream naming, renderer tests, and desktop prompt tests.
- [x] Identify which parts of the apple implementation are genuinely reusable and which are apple-specific.
- [x] Write a short implementation note in the task/PR description naming the existing files to be extended and the intended new files, if any.
- [x] Choose the smallest clean extension that fits the existing architecture; do not redesign the architecture before inspection.

### 1. Introduce a reusable organic-form representation

- [x] Extract a small, reusable way to describe and construct a smooth, closed, normalized 2D organic contour.
- [x] Support meaningful form characteristics such as relative width by height, upper/lower taper, shoulders, lobes, indentations, and restrained asymmetry.
- [x] Support distinct semantic profiles rather than one generic blob with only its color changed.
- [x] Keep geometric construction separate from material appearance: fruit profiles describe shape; material/appearance descriptions control color, shading, and surface detail.
- [x] Keep geometry pure and testable where it fits the current Core/renderer boundary; do not introduce Skia types into Core.
- [ ] Use a smooth closed curve or equivalent robust contour construction; avoid brittle collections of hand-authored renderer path points for every subject.
- [ ] Keep coordinates normalized and bounded, and ensure forms remain stable at all currently supported output sizes.
- [x] Use independent, named random streams for seeded irregularity so new fruit details do not perturb unrelated apple details.
- [x] Avoid building a universal vector graphics system, arbitrary polygon editor, node graph, or plugin architecture.

### 2. Express the three fruit forms through the shared system

- [ ] Refactor the current apple to use the reusable representation without losing its recognizable silhouette, lobes, top indentation, stem attachment, or existing visual quality.
- [x] Add a pear profile with a clearly broad lower body, narrower upper neck, and natural shoulder transition.
- [ ] Verify that the pear silhouette reads as a pear even when rendered as a flat black shape.
- [x] Add an orange profile that is broadly round but not a mathematically perfect circle, with restrained natural irregularity and an appropriate top/bottom feature.
- [ ] Verify that the orange reads as a real orange rather than a generic disk.
- [ ] Give each form independent, bounded shape parameters and seeded asymmetry; avoid excessive wobble or noise.
- [x] Derive attachment points (such as the stem) from the form/profile or explicit attachment data, not unrelated hard-coded screen coordinates.
- [x] Reuse stem/leaf construction where visually appropriate, while allowing fruit-specific details where needed; do not force identical stem/leaf arrangements.

### 3. Separate and reuse the fruit rendering pipeline

- [x] Build on the existing CPU Skia rendering path.
- [x] Reuse common body drawing, contour clipping, lighting, and shadow behavior instead of maintaining three complete independent rendering pipelines.
- [x] Keep fruit-specific appearance in small, explicit material/style descriptions.
- [ ] Preserve coherent scene lighting, volume, highlights, contact shadows, and cast shadows so each fruit sits naturally on the table.
- [ ] Retain the apple's existing smooth, subtly varied skin and accepted polished appearance.
- [ ] Give pears a distinct green/yellow skin appearance, slightly more matte and subtly varied than the apple, without heavy noise.
- [ ] Give the orange a recognizable peel appearance using restrained procedural pores or peel texture.
- [ ] Clip orange surface details to the body contour, scale them sensibly with output size, make them respect lighting, and keep them subtle at normal viewing size.
- [ ] Ensure the orange texture does not resemble polka dots, glitter, or a flat noise overlay.
- [x] Use deterministic, named random streams for every seeded detail; keep geometry, materials, and surface-detail randomness independent.
- [x] Keep the existing wood, background, and overall composition unchanged unless a small adjustment is necessary to support all three fruits consistently.
- [ ] Ensure performance remains suitable for ordinary CPU use at the default 1024 × 1024 output.

### 4. Extend scene construction and the strict prompt grammar

- [x] Extend the existing scene model and builder to represent fruit type and relevant appearance/profile.
- [x] Route all three subjects through the same generation service and renderer entry point.
- [ ] Retain all currently supported apple prompts and their existing behavior.
- [x] Add support for `a green pear on a wooden table`.
- [x] Add support for `a yellow pear on a wooden table`.
- [x] Add support for `an orange on a wooden table`.
- [x] Continue tolerating the existing case, whitespace, and terminal-punctuation variations.
- [x] Reject unsupported object/color combinations with a useful error rather than silently falling back to an apple or default color.
- [x] Keep parser output explicit and validated; do not silently interpret arbitrary extra adjectives or unsupported colors.
- [ ] Keep seed, variation, output dimensions, preview retention on failures, and save metadata working as before.
- [x] Do not add composition controls or new UI panels; the existing prompt field is sufficient for this milestone.

### 5. Add automated coverage

**Core / scene construction**

- [ ] Test that every supported apple, pear, and orange prompt parses to the correct subject and appearance.
- [ ] Test that unsupported objects, colors, and combinations are rejected clearly.
- [ ] Test that organic contours are finite, closed, normalized/bounded, and repeatable for the same inputs.
- [ ] Test that apple, pear, and orange profiles are materially distinct.
- [ ] Include a meaningful pear-profile assertion for the lower-body/upper-neck relationship, not merely enum comparisons.
- [ ] Test that seeded shape variation changes within defined bounds and never produces invalid geometry.
- [ ] Test that named random streams remain isolated: adding or changing orange/pear detail must not consume or shift unrelated apple random values.

**Renderer**

- [ ] Test that each subject renders and encodes a valid PNG at the default size and representative smaller/larger and rectangular sizes already supported by the renderer.
- [ ] Test that identical inputs reproduce identical pixels.
- [ ] Test that different seeds produce controlled differences without changing subject identity.
- [ ] Test that different fruit profiles produce visibly distinct silhouettes.
- [ ] Test that orange peel details remain inside the body contour and do not unexpectedly alter background/table regions.
- [ ] Keep existing apple opacity, grounding, stem, lighting, and wood-grain regression coverage passing.
- [ ] Test that variation extremes remain bounded and render successfully.

**Desktop integration**

- [ ] Extend existing prompt/session/component tests to prove that the new prompts travel through the same real generation path.
- [ ] Verify that invalid prompts retain the previous successful preview and metadata.
- [ ] Preserve and test the existing seed, randomize, disabled-state, and PNG-save behavior.

**Test strategy**

- [ ] Avoid introducing golden-image tests unless the repository already has a suitable, stable image-comparison convention.
- [ ] Prefer targeted geometry/pixel invariants plus generated sample images for visual review.

### 6. Review actual generated output

- [ ] Generate sample PNGs for apple, pear, and orange at the default 1024 × 1024 size.
- [ ] Generate at least one alternate-seed sample for each newly added fruit.
- [ ] Inspect low- and high-variation outputs for obvious shape breakdown.
- [ ] Review the images at normal viewing size, not only zoomed in.
- [ ] Confirm the apple still looks like the previously accepted apple.
- [ ] Confirm the pear is immediately recognizable by its silhouette.
- [ ] Confirm the orange is immediately recognizable by its form and restrained peel detail.
- [ ] Confirm each fruit has convincing volume, coherent highlights/shadows, and believable contact with the table.
- [ ] Confirm surface detail does not overwhelm shape or become distracting visual noise.
- [ ] Confirm different seeds create controlled variations, not malformed subjects.
- [ ] Include generated sample PNGs in the completion report or provide them as reviewable artifacts.
- [ ] Do not commit large generated output files unless the project already has an established convention for them.

### 7. Documentation and verification

- [x] Update README with the new supported prompts and a concise description of organic-form/fruit support.
- [x] Update or add a short milestone handoff describing the reusable system, its boundaries, supported prompts, known limitations, and verification evidence.
- [ ] Update this task checklist accurately; do not mark visual or native checks complete unless they were actually performed.
- [ ] Run the full existing restore/build/test workflow.
- [ ] Ensure the solution builds with warnings as errors and all test projects pass.
- [x] Keep the established CI policy: Linux Release CI is the routine automated gate.
- [x] Do not add a permanent Debug CI job or new runner just for this milestone.
- [ ] Confirm the latest CI run for the completed change passes before reporting completion.

## Acceptance checklist

- [ ] Existing apple prompts and behavior still work, and the apple remains visually acceptable.
- [ ] Green and yellow pears and an orange can be generated through the existing desktop prompt-to-PNG pipeline.
- [ ] The three subjects share a small, genuinely reusable organic-form and rendering foundation while retaining distinct silhouettes and appearances.
- [ ] Pear and orange look recognizably different from the apple and from each other at normal viewing size.
- [ ] Seeded output is deterministic, random streams are isolated, and shape/detail variation remains bounded.
- [ ] Automated Core, renderer, and desktop integration tests cover the new behavior and all existing tests pass.
- [ ] README and milestone handoff accurately describe the new capability and its limits.
- [ ] The completed change passes the repository's Release CI gate.
- [ ] The agent stops and reports implementation decisions, tests/CI evidence, known limitations, and sample PNGs.
- [ ] Do not continue to another subject family or feature milestone without review.

## Explicitly out of scope

- Flowers, plants, trees, mushrooms, or general vegetation.
- Berries, citrus varieties, bananas, grapes, or additional fruit beyond apple/pear/orange.
- Multiple fruits in one scene, bowls, plates, baskets, or more elaborate composition.
- Animals, people, furniture, buildings, landscapes, or other subject families.
- General-purpose object/scene grammar, synonym expansion, free-form natural-language understanding, or an AI prompt translator.
- A generalized material editor, surface/material node system, layer editor, or advanced desktop controls.
- 3D, skeletal/physical simulation, GPU rendering, neural models, model downloads, training, or photorealism claims.
- Large refactors unrelated to organic forms or fixes needed to preserve the existing pipeline.

## Working instruction

Implement this milestone directly in the existing repository, in small coherent steps, with tests as the system grows. Make reasonable engineering decisions without asking the user to design the internal architecture. Do not stop after writing a plan or only documenting the work: complete the scoped implementation. If the current architecture reveals a genuine blocker or a requirement would force a disproportionate redesign, explain the evidence and propose the smallest alternative before expanding scope. Stop at the acceptance checkpoint above.