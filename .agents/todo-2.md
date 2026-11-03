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

The implementation is merged in PR #16. [Post-merge Linux Release CI](https://github.com/Vraxo/Formloom/actions/runs/41584177007) passed restore, warnings-as-errors build, both test projects and artifact uploads at commit `d6aa8ff`. The PR run reported 115 passing tests with no failures or skips. The user's Windows Release restore/build also succeeded after correcting inherited NuGet sources. No local sandbox SDK was installed.

The user supplied all 14 CI PNGs on 2026-11-03. Actual apple, green/yellow pear and orange images, seed 42 variation 0/0.35/1, alternate seed 1337 and black silhouettes were visually inspected. Transparent black body-only silhouettes (no stems/leaves/calyx) were composited onto white for inspection only. Complete-subject silhouette exports are authored in the follow-up below, pending fresh CI and visual review. Visual findings and limitations are recorded in the handoff; no new GUI execution or golden-image pixel comparison is claimed. The scoped visual review passes for this stylized renderer. Generated images remain uncommitted.

## Sequential implementation checklist

**Checklist rules for the agent**

- Work through the sections in order; do not skip ahead to implementation before inspecting the existing code.
- Mark a checkbox `[x]` only after the task is actually completed and verified.
- Keep this file updated in the repository as work progresses, so the user can inspect the remaining work at any point.
- Do not mark a test, visual review, documentation update, or CI check complete merely because it was planned or authored; it must actually be performed.
- If a task is blocked, leave it unchecked and record the blocker and evidence next to it.

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
- [x] Use a smooth closed curve or equivalent robust contour construction; avoid brittle collections of hand-authored renderer path points for every subject.
- [x] Keep coordinates normalized and bounded, and ensure forms remain stable at all currently supported output sizes.
- [x] Use independent, named random streams for seeded irregularity so new fruit details do not perturb unrelated apple details.
- [x] Avoid building a universal vector graphics system, arbitrary polygon editor, node graph, or plugin architecture.

### 2. Express the three fruit forms through the shared system

- [x] Refactor the current apple to use the reusable representation without losing its recognizable silhouette, lobes, top indentation, stem attachment, or existing visual quality.
- [x] Add a pear profile with a clearly broad lower body, narrower upper neck, and natural shoulder transition.
- [x] Verify that the pear silhouette reads as a pear even when rendered as a flat black shape.
- [x] Add an orange profile that is broadly round but not a mathematically perfect circle, with restrained natural irregularity and an appropriate top/bottom feature.
- [x] Verify that the orange reads as a real orange rather than a generic disk.
- [x] Give each form independent, bounded shape parameters and seeded asymmetry; avoid excessive wobble or noise.
- [x] Derive attachment points (such as the stem) from the form/profile or explicit attachment data, not unrelated hard-coded screen coordinates.
- [x] Reuse stem/leaf construction where visually appropriate, while allowing fruit-specific details where needed; do not force identical stem/leaf arrangements.

### 3. Separate and reuse the fruit rendering pipeline

- [x] Build on the existing CPU Skia rendering path.
- [x] Reuse common body drawing, contour clipping, lighting, and shadow behavior instead of maintaining three complete independent rendering pipelines.
- [x] Keep fruit-specific appearance in small, explicit material/style descriptions.
- [x] Preserve coherent scene lighting, volume, highlights, contact shadows, and cast shadows so each fruit sits naturally on the table.
- [x] Retain the apple's existing smooth, subtly varied skin and accepted polished appearance.
- [x] Give pears a distinct green/yellow skin appearance, slightly more matte and subtly varied than the apple, without heavy noise.
- [x] Give the orange a recognizable peel appearance using restrained procedural pores or peel texture.
- [x] Clip orange surface details to the body contour, scale them sensibly with output size, make them respect lighting, and keep them subtle at normal viewing size.
- [x] Ensure the orange texture does not resemble polka dots, glitter, or a flat noise overlay.
- [x] Use deterministic, named random streams for every seeded detail; keep geometry, materials, and surface-detail randomness independent.
- [x] Keep the existing wood, background, and overall composition unchanged unless a small adjustment is necessary to support all three fruits consistently.
- [x] Ensure performance remains suitable for ordinary CPU use at the default 1024 × 1024 output. All ten supplied post-merge CI timing sidecars were assessed: render plus PNG encode took 110.37–183.07 ms on a four-logical-processor Ubuntu runner. Advisory observations, not a desktop latency guarantee or isolated benchmark; see handoff.

### 4. Extend scene construction and the strict prompt grammar

- [x] Extend the existing scene model and builder to represent fruit type and relevant appearance/profile.
- [x] Route all three subjects through the same generation service and renderer entry point.
- [x] Retain all currently supported apple prompts and their existing behavior.
- [x] Add support for `a green pear on a wooden table`.
- [x] Add support for `a yellow pear on a wooden table`.
- [x] Add support for `an orange on a wooden table`.
- [x] Continue tolerating the existing case, whitespace, and terminal-punctuation variations.
- [x] Reject unsupported object/color combinations with a useful error rather than silently falling back to an apple or default color.
- [x] Keep parser output explicit and validated; do not silently interpret arbitrary extra adjectives or unsupported colors.
- [x] Keep seed, variation, output dimensions, preview retention on failures, and save metadata working as before.
- [x] Do not add composition controls or new UI panels; the existing prompt field is sufficient for this milestone.

### 5. Add automated coverage

**Core / scene construction**

- [x] Test that every supported apple, pear, and orange prompt parses to the correct subject and appearance.
- [x] Test that unsupported objects, colors, and combinations are rejected clearly.
- [x] Test that organic contours are finite, closed, normalized/bounded, and repeatable for the same inputs.
- [x] Test that apple, pear, and orange profiles are materially distinct.
- [x] Include a meaningful pear-profile assertion for the lower-body/upper-neck relationship, not merely enum comparisons.
- [x] Test that seeded shape variation changes within defined bounds and never produces invalid geometry.
- [x] Test that named random streams remain isolated: adding or changing orange/pear detail must not consume or shift unrelated apple random values.

**Renderer**

- [x] Test that each subject renders and encodes a valid PNG at the default size and representative smaller/larger and rectangular sizes already supported by the renderer.
- [x] Test that identical inputs reproduce identical pixels.
- [x] Test that different seeds produce controlled differences without changing subject identity.
- [x] Test that different fruit profiles produce visibly distinct silhouettes. Pixel-array distinction passed in CI; supplied silhouettes were visually inspected on white.
- [x] Test that orange peel details remain inside the body contour and do not unexpectedly alter background/table regions.
- [x] Keep existing apple opacity, grounding, stem, lighting, and wood-grain regression coverage passing.
- [x] Test that variation extremes remain bounded and render successfully.

**Desktop integration**

- [x] Extend existing prompt/session/component tests to prove that the new prompts travel through the same real generation path.
- [x] Verify that invalid prompts retain the previous successful preview and metadata.
- [x] Preserve and test the existing seed, randomize, disabled-state, and PNG-save behavior.

**Test strategy**

- [x] Avoid introducing golden-image tests unless the repository already has a suitable, stable image-comparison convention.
- [x] Prefer targeted geometry/pixel invariants plus generated sample images for visual review.

### 6. Review actual generated output

- [x] Generate sample PNGs for apple, pear, and orange at the default 1024 × 1024 size.
- [x] Generate at least one alternate-seed sample for each newly added fruit.
- [x] Inspect low- and high-variation outputs for obvious shape breakdown.
- [x] Review the images at normal viewing size, not only zoomed in.
- [x] Confirm the apple still looks like the previously accepted apple.
- [x] Confirm the pear is immediately recognizable by its silhouette.
- [x] Confirm the orange is immediately recognizable by its form and restrained peel detail.
- [x] Confirm each fruit has convincing volume, coherent highlights/shadows, and believable contact with the table.
- [x] Confirm surface detail does not overwhelm shape or become distracting visual noise.
- [x] Confirm different seeds create controlled variations, not malformed subjects.
- [x] Include generated sample PNGs in the completion report or provide them as reviewable artifacts.
- [x] Do not commit large generated output files unless the project already has an established convention for them.

### 7. Documentation and verification

- [x] Update README with the new supported prompts and a concise description of organic-form/fruit support.
- [x] Update or add a short milestone handoff describing the reusable system, its boundaries, supported prompts, known limitations, and verification evidence.
- [x] Update this task checklist accurately; do not mark visual or native checks complete unless they were actually performed.
- [x] Run the full existing restore/build/test workflow.
- [x] Ensure the solution builds with warnings as errors and all test projects pass.
- [x] Keep the established CI policy: Linux Release CI is the routine automated gate.
- [x] Do not add a permanent Debug CI job or new runner just for this milestone.
- [x] Confirm the latest CI run for the completed change passes before reporting completion.

## Acceptance checklist

- [x] Existing apple prompts and behavior still work, and the apple remains visually acceptable.
- [x] Green and yellow pears and an orange can be generated through the existing desktop prompt-to-PNG pipeline.
- [x] The three subjects share a small, genuinely reusable organic-form and rendering foundation while retaining distinct silhouettes and appearances.
- [x] Pear and orange look recognizably different from the apple and from each other at normal viewing size.
- [x] Seeded output is deterministic, random streams are isolated, and shape/detail variation remains bounded.
- [x] Automated Core, renderer, and desktop integration tests cover the new behavior and all existing tests pass.
- [x] README and milestone handoff accurately describe the new capability and its limits.
- [x] The completed change passes the repository's Release CI gate.
- [x] The agent stops and reports implementation decisions, tests/CI evidence, known limitations, and sample PNGs. Reports and user-supplied PNG reviews are recorded in the handoff; final timing assessment closes the remaining implementation acceptance gate.
- [x] Do not continue to another subject family or feature milestone without review. Stopped at the Phase 2 checkpoint; no next milestone started.

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
## Follow-up: complete subject silhouettes

- [x] Author a shared renderer silhouette mode including stems/leaves/calyx, without scene/shadow content.
- [x] Distinguish body-only and complete-subject export filenames; add body-mask presentation padding.
- [x] Author attachment/body inclusion, repeatability and transparent-border coverage.
- [x] Run Release CI for the silhouette follow-up: PR run 41598590278 passed after the Skia sampling-overload fix; post-merge run 41598698403 passed at `f53b8cd`, including restore, warnings-as-errors build, tests and preview upload.
- [x] Inspect newly exported complete-subject silhouettes, including attachments, after CI: user supplied apple/pear/orange exports for seeds 42/1337 on 2026-11-03; stems/leaves are present on apple/pear, outlines are recognizable and clear of canvas edges.

This follow-up changes the previously documented body-only export limitation; it is not a new subject family. Existing normal-render regression tests passed in the follow-up CI. Visual inspection of the supplied complete-subject silhouettes passes. The supplied body-only mask in this batch is pear; no new visual claim is made for absent apple/orange body-mask exports.


## Final acceptance evidence reconciliation

Desktop pipeline acceptance is supported by passing real `GenerationService`/`DesktopSession` tests for green pear, yellow pear and orange, including failed-prompt preview retention and saved-image metadata. This is not a new native-window smoke test. Shared construction and deterministic/bounded output acceptance are supported by the reviewed source, passing Core/render tests and inspected samples. Latest merged documentation CI run 41603169902 passed at `327d25c`; it predates the timing instrumentation authored here. The ten supplied timing reports have now been assessed (110.37–183.07 ms per 1024px render plus encoding), closing the remaining performance evidence gate. Post-merge timing implementation CI run 41612817131 passed at `3c4c624`. The final evidence documentation changes are uncommitted; prior CI validates the implementation, not these new documentation edits.
