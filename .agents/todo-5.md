# Formloom — Phase 5: Fruit Containment in Ceramic Bowls

## Objective

Teach Formloom to depict one existing fruit placed **inside** one existing ceramic bowl. This is the first semantic spatial relationship in the project: the bowl is a container, and the fruit is its content, not a neighboring object that happens to overlap it.

The result must use the existing prompt → Core scene construction → deterministic placement → Skia CPU renderer → PNG/save pipeline. The bowl's rear rim and cavity must sit behind the fruit; the bowl's near/front wall and lip must visibly occlude the lower part of the fruit. The fruit must look seated within the cavity, at a scale that makes sense for the bowl, with subtle interior contact shading.

This milestone deliberately supports exactly **one bowl containing exactly one fruit**. It does not add multiple contents, other container types, arbitrary object relations, or a general scene graph.

## Starting point

- Current repository: `https://github.com/Vraxo/Formloom`, branch `main`.
- Start from the latest `main`; inspect the actual `HEAD` and working tree before editing.
- Phase 4 checklist: `.agents/todo-4.md`.
- Phase 4 handoff: `docs/ceramic-vessels-handoff.md`.
- Current architecture includes `Formloom.Core`, `Formloom.Rendering.Skia`, `Formloom.Desktop`, `Formloom.Tests`, and `Formloom.Desktop.Tests`; .NET 10 / C# 14; PhotinoX.Blazor; SkiaSharp CPU rendering; and an existing Linux Release CI workflow.
- Phase 4 added typed `FruitInstance` and `CeramicInstance` data, a closed `SceneEntry` projection, normalized vessel profiles, `MixedComposition`, a shared depth-sorted renderer, ceramic cavity/rim layers, prompt parsing through `ParseObjects`, subject-token whitelisting, and the `procedural-preview` test artifact.

## Exact scope and prompt vocabulary

### Existing vocabulary — preserve exactly

All existing fruit descriptors, bowl descriptors, other vessel descriptors, single-object prompts, fruit groups, ceramic groups, and mixed still-life prompts must continue to work unchanged.

Fruit descriptors:

- `a red apple`
- `a green apple`
- `a yellow apple`
- `a green pear`
- `a yellow pear`
- `an orange`

Bowl descriptors:

- `a blue ceramic bowl`
- `a cream ceramic bowl`

### New containment prompts

Support the exact grammar:

`<one existing fruit descriptor> in <one existing bowl descriptor> on a wooden table`

Examples:

- `a red apple in a blue ceramic bowl on a wooden table`
- `a green apple in a cream ceramic bowl on a wooden table`
- `a yellow apple in a blue ceramic bowl on a wooden table`
- `a green pear in a cream ceramic bowl on a wooden table`
- `a yellow pear in a blue ceramic bowl on a wooden table`
- `an orange in a blue ceramic bowl on a wooden table`

All six existing fruit descriptors must work with **both** bowl glazes, for twelve valid fruit/glaze combinations total. Continue the existing case normalization, whitespace normalization, and terminal-punctuation behavior.

The relationship is always expressed as `fruit in bowl`, with exactly one fruit and exactly one bowl. The prompt order is intentional: the fruit is the content and the bowl is the container. Do not interpret unrelated list syntax as containment.

## Project constraints

- [x] Preserve the current stack, target framework, package versions, solution/project boundaries, analyzer policy, and CI workflow unless an evidence-based blocker requires a small compatible change.
- [x] Keep generation offline, CPU-first, non-neural, seeded, deterministic, and inspectable.
- [x] Keep Core independent of Skia, pixels, bitmaps, native resources, and desktop/UI types.
- [x] Reuse the current prompt → Core → generation service → Skia → PNG/save path.
- [x] Preserve every existing supported prompt, parser contract, image behavior, deterministic stream, subject token, and save filename.
- [x] Preserve the accepted apple, pear, orange, bowl, cup, and vase geometry/material appearance. Do not redesign existing objects merely to make containment easier.
- [x] Keep the existing desktop controls. Do not add manual positioning, scale, rotation, layering, or material controls.
- [x] Add no downloaded images, stock textures, fonts, network dependencies, models, packages, projects, or frameworks.
- [x] Do not introduce a generic scene graph, arbitrary relationship engine, physics solver, 3D renderer, or extensible object registry.
- [x] Keep this milestone limited to one existing bowl containing one existing fruit, on the existing wooden table.

## Sequential implementation checklist

### 0. Inspect and record the actual starting point

- [x] Start from the latest `main`; record the exact starting commit and confirm the working tree is clean.
- [x] Read `.agents/todo-4.md` and `docs/ceramic-vessels-handoff.md` in full.
- [x] Inspect the current Core scene/instance types, `SceneEntry`, scene validation, `PromptParser`, `SceneGenerator`, `MixedComposition`, bowl profile/bounds, and random-stream derivation.
- [x] Trace an existing fruit-only prompt and an existing fruit-plus-bowl prompt end-to-end through parsing, construction, composition, rendering, metadata, desktop session, and PNG saving.
- [x] Read the bowl renderer's current draw order and identify the exact rear-rim, cavity, front-wall, front-lip, highlight, and shadow stages.
- [x] Inspect the existing renderer diagnostic passes, silhouette helpers, pixel-invariant tests, desktop/component tests, and `procedural-preview` artifact exporter.
- [x] Record which existing fruit draw operations can be reused with a local transform and which assume tabletop grounding or ordinary scene depth.
- [x] Write a short implementation note in the PR description naming the smallest Core, parser, renderer, desktop, and test changes.
- [x] Choose a small, explicit typed containment representation that fits the current model; do not begin with a generic relationship abstraction.

### 1. Model the containment relationship in Core

- [x] Represent a contained fruit as a typed child/content of a bowl, or as an equally small explicit typed bowl-containment value attached to the scene.
- [x] Preserve the existing independent `FruitInstance` and `CeramicInstance` paths for ordinary scenes.
- [x] Keep the relationship explicit: the renderer and validator must be able to distinguish a fruit inside a bowl from two unrelated overlapping scene objects.
- [x] Carry the fruit's existing species/color identity and a stable per-instance seed into the containment data.
- [x] Represent the content's position and scale in a normalized bowl-local coordinate system, not as hard-coded output pixels.
- [x] Keep the bowl's existing scene/world placement separate from the fruit's bowl-local placement.
- [x] Derive the content's visible bounds from its fruit profile and local transform; include the relevant bounds in validation and diagnostics.
- [x] Reuse existing fruit profile/material data rather than creating a second, simplified set of fruit shapes for bowl contents.
- [x] Keep the content representation closed and typed. Do not use `object`, reflection, raw type-name strings, or a registry.
- [x] Do not turn contained fruit into an ordinary independently depth-sorted neighbor if doing so would break the required bowl layer order.
- [x] Validate that containment references exactly one bowl and exactly one supported fruit.
- [x] Reject containment on cups and vases; only the existing bowl type is a container in this milestone.
- [x] Reject empty contents, multiple contents, multiple containers, nested containment, and invalid fruit/bowl values.
- [x] Ensure a containment scene cannot also smuggle in extra independent objects; the supported containment composition is exactly one bowl plus one fruit.
- [x] Preserve existing one-to-three independent scene-item rules and compatibility accessors for all non-containment prompts.
- [x] Keep validation errors explicit and actionable rather than silently dropping invalid content.

### 2. Add deterministic bowl-local placement

- [x] Add a small deterministic placement routine specifically for one fruit in one bowl, reusing current normalized bowl-profile and fruit-profile data.
- [x] Place the fruit relative to the bowl's interior/cavity geometry, not relative to the canvas center or an unrelated composition slot.
- [x] Choose a bowl-local scale that lets apple, pear, and orange each read clearly while fitting plausibly inside the opening.
- [x] Give each fruit type an appropriate fit; do not force a pear and an orange to share identical width/height scaling.
- [x] Position the fruit's base near the cavity floor so it looks seated rather than floating above the bowl.
- [x] Keep the fruit horizontally inside the bowl's usable inner width with a safe margin at both side walls.
- [x] Let the fruit rise visibly above the rear rim when appropriate, while ensuring the front rim hides the lower portion convincingly.
- [x] Keep the fruit's transform bounded at variation 0 and 1 and across representative seeds.
- [x] Allow restrained seeded variation in local position and size without changing fruit identity, bowl identity, or containment.
- [x] Use a named containment-placement random stream independent from fruit geometry/material/detail and bowl geometry/glaze streams.
- [x] Derive a stable content-local seed so changing the bowl glaze does not unnecessarily change the fruit's silhouette or skin detail.
- [x] Ensure identical prompt + seed + variation produce identical local placement and pixels on the same supported runtime.
- [x] Ensure alternate seeds can produce modest, coherent placement differences without wall collisions, spills, or loss of visibility.
- [x] Do not use physics, collision simulation, a solver, or arbitrary position search; use explicit profile-aware constraints.

### 3. Render the fruit genuinely inside the bowl

- [x] Extend the existing Skia renderer and keep one scene background/table and one established PNG pipeline.
- [x] Reuse the existing fruit body, material, stem/leaf/calyx, lighting, and detail drawing code with the bowl-local transform.
- [x] Do not create separate hand-painted fruit silhouettes or a special low-quality content renderer.
- [x] Split or expose the bowl's existing draw stages as narrowly as needed to establish correct containment layering.
- [x] Draw the bowl's rear rim/far wall and cavity before drawing the contained fruit.
- [x] Draw the fruit after the rear cavity surfaces so it appears inside the opening rather than painted onto the front of the bowl.
- [x] Draw the bowl's near/front wall and front lip after the fruit so they visibly occlude the lower fruit body.
- [x] Preserve the correct bowl outline, rim thickness, cavity gradient, far-wall sheen, front lip, glaze highlights, and foot.
- [x] Ensure the fruit does not appear in front of the bowl's front lip or pasted over the bowl's exterior.
- [x] Ensure the fruit does not leak outside the usable inner bowl width or appear detached above the cavity.
- [x] Use the existing bowl cavity geometry/masks where suitable; do not substitute a flat ellipse or arbitrary rectangular clip.
- [x] Keep the fruit's original material/detail clipping to its own body geometry.
- [x] Keep fruit detail from appearing on the bowl, table, or background.
- [x] Add a subtle localized contact/ambient-occlusion shadow on the cavity floor beneath the fruit.
- [x] Clip the interior contact shadow to the bowl's cavity region so it cannot leak onto the exterior/table.
- [x] Keep the interior shadow visually subordinate to the bowl cavity and fruit; do not create a black halo.
- [x] Do not add a normal tabletop contact shadow beneath the contained fruit; the bowl remains the object grounded on the table.
- [x] Preserve the bowl's existing tabletop contact/cast shadow and scene light direction.
- [x] Ensure the bowl's front lip and body occlude the fruit in both normal rendering and relevant diagnostic/silhouette passes.
- [x] Define and document how a complete-subject silhouette represents a bowl plus its contents; preserve the existing bowl-cavity silhouette convention.
- [x] Ensure all temporary Skia resources are disposed and no static native-resource caches are introduced.
- [x] Preserve all existing standalone bowl, cup, vase, fruit, and independent mixed-scene rendering behavior; inspect any regression before accepting it.

### 4. Extend the strict prompt grammar and generation pipeline

- [x] Add a specific typed parse result for a fruit-in-bowl prompt, or the smallest equivalent extension of the existing object parser.
- [x] Parse the six existing fruit descriptors in the content position.
- [x] Parse both existing bowl descriptors in the container position.
- [x] Support all twelve fruit-descriptor × bowl-glaze combinations.
- [x] Require the exact relationship word `in` between the fruit and the bowl descriptor.
- [x] Require the existing `on a wooden table` setting phrase.
- [x] Preserve the existing case, whitespace, and terminal-punctuation normalization rules.
- [x] Preserve the semantic distinction between the fruit (content) and the bowl (container) in the parsed/Core representation.
- [x] Route valid containment prompts through the existing generation service and renderer entry point.
- [x] Keep existing `Parse`, `ParseFruit`, `ParseScene`, and `ParseObjects` contracts compatible; if a parser entry point must be extended, document why and preserve existing callers/tests.
- [x] Reject `in` relationships with cups or vases.
- [x] Reject reversed or ambiguous wording such as `a blue ceramic bowl with a red apple`, `a bowl containing an apple`, or `a bowl and an apple` unless already separately supported as ordinary independent objects.
- [x] Reject multiple fruits, multiple bowls, lists, plural forms, quantity words, extra adjectives, unsupported colors, missing `ceramic`, and any unsupported spatial preposition.
- [x] Reject a malformed containment prompt as a whole; never discard an item and silently generate a simpler scene.
- [x] Preserve every existing standalone, fruit-group, ceramic-group, and mixed-scene prompt exactly.
- [x] Preserve seed/variation snapshots, randomize behavior, background generation, duplicate-action guards, error handling, and previous-preview retention on failure.
- [x] Preserve last-successful-image metadata when controls are edited without regenerating.
- [x] Add a safe deterministic whitelisted subject token for fruit-in-bowl images; include fruit identity and bowl identity/glaze if that matches current naming conventions.
- [x] Update filename whitelist and tests; never derive filenames from raw prompt text.
- [x] Preserve every existing fruit, fruit-group, ceramic, ceramic-group, and still-life-group filename/token unchanged.
- [x] Update prompt help with one concise fruit-in-bowl example; do not add desktop controls or panels.

### 5. Add Core and parser tests

- [x] Test all twelve supported fruit-in-bowl prompt combinations.
- [x] Test case normalization, repeated whitespace, terminal punctuation, and the existing exact grammar behavior.
- [x] Test that the parsed fruit type/color and bowl glaze match the requested descriptors exactly.
- [x] Test that the parse result preserves the content/container roles and cannot be mistaken for an ordinary independent-object list.
- [x] Test rejection of cup/vase containment, reversed wording, unsupported colors, unsupported fruit, extra objects, plural/quantity forms, malformed separators, and extra adjectives.
- [x] Test that an invalid containment prompt is rejected as a whole.
- [x] Test Core validation for exactly one supported bowl and exactly one supported fruit content.
- [x] Test rejection of no bowl, no fruit, multiple bowls, multiple contents, nested containment, and extra independent scene entries.
- [x] Test finite, normalized, bounded bowl-local transforms and content bounds for apple, pear, and orange.
- [x] Test that each fruit's local bounds fit within the bowl's allowed interior width with a defined safety margin.
- [x] Test that the fruit base is within a bounded distance of the cavity floor and is not placed above the opening as if floating.
- [x] Test seeded determinism of local placement.
- [x] Test that changing seed can alter placement while preserving the same fruit and bowl descriptors.
- [x] Test that changing only bowl glaze does not shift the content geometry/detail streams unnecessarily.
- [x] Test that varying the seed/variation across a representative matrix never produces an invalid placement.
- [x] Retain all existing parser, scene-validation, fruit-profile, ceramic-profile, and composition tests.

### 6. Add renderer pixel/invariant and desktop integration tests

- [x] Test valid PNG output for all three fruit types in both bowl glazes at 1024 × 1024.
- [x] Test representative smaller, larger, and rectangular output dimensions already supported by the renderer.
- [x] Test identical containment inputs produce identical decoded pixels on the same supported runtime.
- [x] Test alternate seeds produce bounded visible differences without changing the requested fruit/bowl identities.
- [x] Add a targeted layering invariant proving the rear rim/cavity is behind the fruit and the front lip is in front of it.
- [x] Ensure the layering test has a real overlap/pixel count and cannot pass vacuously when the objects do not overlap.
- [x] Test that the visible fruit region lies within the bowl opening/interior's allowed horizontal bounds.
- [x] Test that the lower fruit region is occluded by the bowl's front wall/lip while a meaningful upper portion remains visible.
- [x] Test that fruit pixels/details do not leak onto the bowl exterior, tabletop, or background.
- [x] Test that the fruit remains recognizable and its species silhouette is not distorted by the local transform.
- [x] Test that the cavity contact shadow exists beneath the fruit, is bounded, and remains clipped to the cavity.
- [x] Test that the contained fruit does not receive an independent tabletop shadow.
- [x] Test that the bowl retains its existing outer contact/cast shadow, direction, and canvas-edge safety.
- [x] Test the complete-subject silhouette convention for the bowl plus contained fruit and ensure intentional cavity areas are handled consistently.
- [x] Test `RenderWithoutInstanceDetail` or the equivalent diagnostic path to ensure it suppresses content detail without changing subsequent normal-render output.
- [x] Retain all existing fruit, ceramic, group-composition, clipping, shadow, silhouette, PNG, resource-ownership, and repeatability tests.
- [x] Extend `GenerationService`/`DesktopSession` tests to submit a valid fruit-in-bowl prompt through the actual generation path.
- [x] Extend rendered-component tests to submit at least one apple-in-bowl and one non-apple-in-bowl prompt.
- [x] Test that an unsupported containment prompt retains the previous successful preview and matching save metadata.
- [x] Test the new subject token/filename and confirm every existing filename remains unchanged.

### 7. Generate visual-review artifacts

- [x] Extend the existing `procedural-preview` artifact; do not add another artifact workflow without a demonstrated need.
- [x] Generate default-seed/default-variation PNGs for apple in blue bowl, apple in cream bowl, pear in blue bowl, pear in cream bowl, orange in blue bowl, and orange in cream bowl.
- [x] Generate at least one alternate-seed render for each fruit type.
- [x] Generate low- and high-variation examples for representative fruit/bowl combinations.
- [x] Generate complete-subject silhouettes for representative containment scenes using the documented silhouette convention.
- [x] Generate a compact labeled review sheet or an accompanying exact filename/order manifest so each fruit, glaze, seed, and variation can be identified unambiguously.
- [x] Include a small number of individual full-resolution PNGs alongside the review sheet.
- [x] Keep generated PNGs and review composites in ignored test-output/artifact directories; do not commit generated binaries as source assets.
- [x] Generate timing sidecars for representative 1024 × 1024 containment cases if the existing exporter already supports them.
- [ ] Inspect actual generated pixels where the working environment allows it; generating/uploading a PNG alone is not a visual review.
- [x] If the agent cannot access the actual generated pixels, leave the human visual-review gate pending and provide the exact artifact, filename, and review-sheet details; do not fabricate a visual pass.
- [ ] Review that the fruit reads as sitting inside the bowl, not floating, pasted on the front, or placed behind the entire bowl.
- [ ] Review that the front rim clearly occludes the lower fruit and the fruit is still recognizably an apple, pear, or orange.
- [ ] Review that cavity shading and interior contact shadow reinforce depth without making the bowl muddy or excessively dark.
- [ ] Review both blue and cream glaze variants, alternate seeds, and variation extremes for clipping or visual breakdown.

### 8. Documentation, verification, and handoff

- [x] Update the README current-capability section to mention fruit-in-bowl containment.
- [x] Document the exact accepted fruit-in-bowl prompt grammar and the twelve supported combinations (six fruit descriptors × two bowl glazes).
- [x] Document that only bowls can contain fruit in this milestone, with exactly one fruit per bowl.
- [x] Document that cups, vases, multiple contents, liquid, and arbitrary spatial language remain unsupported.
- [x] Update the README architecture overview only as much as needed to explain the explicit containment representation and bowl-local rendering.
- [x] Add `docs/containment-handoff.md` with starting/ending commits, design decisions, typed relationship model, local placement, render-layer order, grammar, metadata, tests, CI evidence, artifacts, timing observations, visual-review status, and known limitations.
- [x] Keep `.agents/todo-5.md` updated throughout implementation.
- [x] Mark a checkbox `[x]` only after its work is complete and the stated verification has actually been performed.
- [x] Leave blocked, unexecuted, or human-review tasks unchecked and explain the blocker/evidence beside them.
- [x] Run the established solution restore, Release warnings-as-errors build, and full solution test workflow through the existing CI gate.
- [x] Confirm all existing and new test projects pass on the latest PR CI run.
- [x] Confirm the post-merge Linux Release CI run passes before closing the milestone.
- [x] Do not add a permanent Debug job, Windows runner, package, project, or workflow solely for this milestone.
- [x] Do not claim native Photino/WebView2 dialog, window-lifecycle, resize, or accessibility testing from headless component tests or Linux CI.
- [x] Record exact PR, merge commit, and passing CI run URLs in the handoff.
- [x] Reconcile this checklist with the handoff and actual changed files; identify anything authored but not executed.
- [x] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, visual-review status, artifact location, timing observations, and known limitations.

## Acceptance checklist

- [x] All existing fruit, bowl, cup, vase, fruit-group, ceramic-group, and mixed-scene prompts still work unchanged.
- [x] All twelve fruit-in-bowl prompt combinations parse and generate through the existing desktop prompt-to-PNG path.
- [x] The Core model represents the fruit as content of the bowl, not as an unrelated overlapping scene neighbor.
- [x] The fruit is positioned in bowl-local coordinates, fitted to the bowl cavity, and remains bounded across seeds and variation.
- [x] The bowl's rear rim/cavity is behind the fruit and its front wall/lip visibly occludes the lower fruit.
- [ ] The fruit remains recognizable, correctly shaded, and visually seated inside the cavity.
- [x] A subtle cavity contact shadow grounds the fruit without leaking onto the bowl exterior or tabletop.
- [x] Blue and cream bowls both work with apple, pear, and orange.
- [x] Identical inputs are deterministic; alternate seeds create coherent bounded changes.
- [x] Existing independent composition, fruit-only rendering, ceramic rendering, metadata, and save behavior remain intact.
- [x] Core, renderer, desktop-session, and rendered-component tests cover containment, and all existing tests pass.
- [x] Generated visual-review artifacts are available; the handoff accurately distinguishes automated evidence from inspected pixels.
- [x] README, prompt help, safe save metadata, and handoff describe the implemented feature and its limits accurately.
- [x] The completed change passes the existing Linux Release CI gate and the post-merge run is recorded.
- [x] The agent stops and reports the results; it does not begin a new object family or relationship type.

## Explicitly out of scope

- Two or more fruits inside one bowl.
- Fruit inside cups or vases; objects inside any other vessel.
- Bowls inside bowls, nested containers, or arbitrary containment depth.
- Liquid, soup, cereal, food piles, pouring, splashing, or fluid simulation.
- Fruit placed on top of, beside, behind, or leaning against vessels as a newly requested spatial relation.
- Multiple independent objects plus a contained fruit in the same prompt; containment scenes contain exactly one bowl and one fruit.
- User-authored positioning, left/right/behind/in-front controls, rotation/scale controls, or a scene editor.
- New object families, new fruit types, new ceramic types, plates, cutlery, flowers, or decorations.
- A general-purpose relationship language, unrestricted natural-language parsing, synonym expansion, or AI prompt translation.
- A generic scene graph, physics, collision engine, 3D geometry, cameras, GPU rendering, neural models, downloaded textures, or photorealism claims.
- Unrelated architecture refactors or broad renderer rewrites.

## Working instruction

Implement this milestone directly in the existing repository, starting from the latest `main`, in small coherent steps with tests as the feature grows. Make reasonable engineering decisions without asking the user to design internal architecture. Do not stop after planning, documentation, or placeholder types: complete the scoped implementation.

Prefer the smallest explicit typed containment model and the existing bowl/fruit drawing primitives. Preserve compatibility with all existing prompt and rendering paths. If inspection reveals a genuine blocker, document the concrete evidence and implement the smallest compatible alternative rather than broadening the scope. Keep this file current as work proceeds. Stop after the acceptance checklist and provide a concise, evidence-based completion report.
