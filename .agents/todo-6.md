# Formloom — Phase 6: Still Liquid in Ceramic Cups

## Objective

Teach Formloom to depict one existing ceramic cup containing one still beverage: **coffee or tea**. The liquid must read as a visible surface inside the existing cup opening, below the ceramic rim, with convincing depth, restrained reflections and a clear distinction between the two drinks.

The result must use the existing prompt → Core scene construction → deterministic generation → Skia CPU renderer → PNG/save pipeline. This is a static illustrated still-life surface, not a fluid simulation.

The milestone supports exactly **one existing cup containing exactly one beverage**. Both existing cup glazes (blue and cream) must work with both beverages, for four valid combinations.

## Starting point

- Repository: `https://github.com/Vraxo/Formloom`, branch `main`.
- Start from the latest `main`; inspect the actual `HEAD`, working tree, and current project layout before editing.
- Previous milestone checklist: `.agents/todo-5.md`.
- Previous milestone handoff: `docs/containment-handoff.md`.
- Current source projects live under `src/`; tests live under `tests/`.
- Stack: .NET 10 / C# 14, PhotinoX.Blazor desktop, SkiaSharp CPU rendering, xUnit v3, existing Linux Release CI.
- Relevant current implementation: `src/Formloom.Core/CeramicProfile.cs`, `CeramicForms.cs`, `SceneGenerator.cs`, `PromptParser.cs`, `StillLifeScene.cs`, `SceneValidator.cs`; `src/Formloom.Rendering.Skia/StillLifeRenderer.cs`; `tests/Formloom.Tests/CeramicRenderingTests.cs`; and the existing desktop generation/component tests.
- The cup profile already exposes normalized `MouthCenter`, `MouthRadius`, `CavityDepth`, and `RimThickness`. The renderer's `DrawCeramic` calls `DrawMouth` while clipping to the ceramic outline. `DrawMouth` currently paints the cavity gradient and interior shading, then the mouth/rim strokes. The liquid should be integrated into this existing cup-specific mouth rendering stage.
- Phase 5's `BowlContainment` is a deliberate, narrowly typed bowl/fruit relation. Do not generalize it into a universal containment engine for this feature.

## Exact scope and prompt vocabulary

### Existing vocabulary — preserve exactly

Every existing supported prompt must continue to work unchanged:

- All six fruit descriptors and fruit-only groups.
- Blue and cream ceramic bowls, cups, and vases.
- Existing pairs, triples, and mixed still-life groups.
- All twelve fruit-in-bowl prompts from Phase 5.
- All existing subject tokens, save filenames, seed/variation behavior, and rejected-prompt behavior.

### New cup/beverage prompts

Support exactly this grammar:

`<one existing cup descriptor> filled with <one beverage> on a wooden table`

Existing cup descriptors:

- `a blue ceramic cup`
- `a cream ceramic cup`

New beverage descriptors:

- `coffee`
- `tea`

Four valid combinations:

- `a blue ceramic cup filled with coffee on a wooden table`
- `a cream ceramic cup filled with coffee on a wooden table`
- `a blue ceramic cup filled with tea on a wooden table`
- `a cream ceramic cup filled with tea on a wooden table`

Continue tolerating the existing case normalization, repeated-whitespace normalization, and terminal-punctuation behavior. Keep the exact grammar strict; do not add synonyms or free-form prompt understanding.

### Explicitly unsupported in this milestone

- Coffee or tea in bowls or vases.
- Fruit in a cup, liquid in a bowl, or any new containment relationship.
- Multiple cups, multiple beverages, or a cup combined with independent objects in the same prompt.
- Multiple liquid layers, mixed drinks, milk, cream, foam, crema art, latte art, bubbles, ice, lemon, tea bags, spoons, saucers, or straws.
- Steam, pouring, splashing, ripples that imply motion, fluid simulation, physics, or animation.
- Arbitrary fill levels, camera controls, material controls, or new desktop UI panels.
- Other drinks, vessel types, colors, adjectives, quantities, plurals, or positional language.

## Project constraints

- [x] Preserve the existing stack, target framework, package versions, solution/project boundaries, analyzer policy, and CI workflow unless a concrete blocker justifies a small compatible change.
- [x] Keep generation offline, CPU-first, non-neural, seeded, deterministic, and inspectable.
- [x] Keep Core independent of Skia, pixels, bitmaps, native resources, and desktop/UI types.
- [x] Reuse the existing prompt → Core → generation service → Skia → PNG/save path.
- [x] Preserve every existing supported prompt, parser contract, image path, deterministic stream, subject token, and save filename.
- [x] Preserve the accepted visual appearance of empty cups, bowls, vases, fruit, mixed scenes, and Phase 5 fruit-in-bowl scenes.
- [x] Keep the existing desktop controls; do not add manual fill-level, liquid-color, reflection, or material controls.
- [x] Add no downloaded images, stock textures, fonts, network dependencies, models, packages, projects, frameworks, or CI runners.
- [x] Do not introduce a general-purpose liquid/material system, fluid solver, physics engine, scene graph, arbitrary relationship engine, or 3D renderer.
- [x] Keep this milestone limited to one still coffee or tea surface inside one existing cup on the existing wooden table.
- [x] Use the current cup mouth geometry as the source of truth; do not hard-code canvas-pixel positions for the liquid.
- [x] Treat the drink as a contained surface/material associated with the cup, not as an independent scene object that casts a tabletop shadow or participates in ordinary depth sorting.

## Sequential implementation checklist

### 0. Inspect and record the actual starting point

- [x] Start from the latest `main`; record the exact starting commit and confirm the working tree is clean.
- [x] Read `.agents/todo-5.md` and `docs/containment-handoff.md` in full.
- [x] Inspect the current `StillLifeScene`, `CeramicInstance`, `SceneEntry`, scene validation, parser, generator, subject-token construction, and save-filename whitelist.
- [x] Trace an empty-cup prompt end-to-end through parsing, Core construction, renderer, generation service, desktop session, preview metadata, and PNG saving.
- [x] Inspect `CeramicProfile` and the cup profile's `MouthCenter`, `MouthRadius`, `CavityDepth`, `RimThickness`, outline and handle geometry.
- [x] Read `StillLifeRenderer.DrawCeramic` and `DrawMouth` in full, including the outline clip, cavity gradient, inner shading, ellipse/rim strokes, handle rendering, and all save/restore boundaries.
- [x] Inspect `RenderSilhouette`, `RenderShadowMask`, and `RenderWithoutInstanceDetail` to understand how a filled cup should behave in diagnostic passes.
- [x] Inspect existing ceramic pixel tests, silhouette/handle-hole tests, desktop generation tests, component tests, PNG filename tests, and the `procedural-preview` exporter.
- [x] Record which existing mouth-rendering operations can be reused without changing empty-cup output and where the beverage surface must be inserted relative to the cavity and rim.
- [x] Write a short implementation note in the PR description naming the smallest Core, parser, renderer, desktop, test, and documentation changes.
- [x] Choose a small explicit typed cup-content representation; do not begin with a generic container/content abstraction.

### 1. Model cup contents explicitly in Core

- [x] Add a small closed beverage type with exactly `Coffee` and `Tea` values.
- [x] Add an explicit typed cup-contents value that associates one existing `CeramicInstance` of kind `Cup` with one beverage kind.
- [x] Keep the beverage value in Core free of Skia colors, paints, shaders, pixels, and rendering resources.
- [x] Keep the existing `CeramicInstance` and `SceneEntry` representations intact for ordinary empty vessels and mixed scenes.
- [x] Add one narrowly scoped optional cup-contents property to `StillLifeScene`, or an equally small typed representation that fits the current scene model.
- [x] Keep the cup itself as the scene's one ceramic entry; do not add the liquid as a second independently positioned scene object.
- [x] Ensure the typed contents value refers to the exact cup represented by the scene, with no duplicated or contradictory cup data.
- [x] Preserve the cup's existing kind, glaze, body transform, seed, depth, profile and handle.
- [x] Keep cup contents mutually exclusive with Phase 5 `BowlContainment`; a scene cannot be both a filled cup and a fruit-in-bowl containment scene.
- [x] Validate that the container is exactly one supported ceramic cup with blue or cream glaze.
- [x] Validate that the beverage is exactly coffee or tea.
- [x] Reject a missing cup, non-cup vessel, missing beverage, invalid beverage value, duplicate cup, extra independent scene entries, and contradictory containment state.
- [x] Keep existing empty-vessel and one-to-three independent-object scene rules unchanged.
- [x] Keep validation errors explicit and actionable; never silently discard the beverage or fall back to an empty cup.
- [x] Derive the liquid's normalized surface geometry from the existing cup profile's opening ellipse and rim thickness, not from arbitrary screen coordinates.
- [x] Keep geometry and beverage appearance separate: Core describes the beverage identity and, if needed, normalized surface bounds; Skia owns the rendered palette and shading.
- [x] Avoid a generalized liquid framework, material registry, type-name strings, reflection, or an `object` field.

### 2. Define a restrained, profile-aware liquid surface

- [x] Calculate the liquid surface from `CeramicProfile.MouthCenter` and `MouthRadius` for the cup.
- [x] Keep the surface inside the usable inner mouth ellipse with a visible ceramic rim/interior margin on every side.
- [x] Account for `RimThickness` so the liquid does not cover the ceramic lip or appear painted over the rim.
- [x] Use a flattened ellipse consistent with the existing cup opening perspective; do not draw a circular disk or a horizontal rectangle.
- [x] Place the liquid slightly below the top lip so the cup reads as filled but not overflowing.
- [x] Keep the surface bounds finite, normalized, positive, and safely inside the opening across all supported output sizes.
- [x] Use one fixed, believable fill level for both drinks in this milestone; no prompt-controlled or UI-controlled volume.
- [x] Keep the amount of visible inner wall consistent with the cup's existing cavity depth.
- [x] Avoid exposing a large empty cavity behind the surface or filling the entire opening edge-to-edge.
- [x] Make the surface geometry independent of cup glaze so switching blue ↔ cream does not shift or resize the liquid.
- [x] If any seeded surface irregularity is genuinely needed, put it on a dedicated named stream and bound it tightly; a static deterministic surface is preferred over decorative randomness.
- [x] Ensure variation 0 and variation 1 both preserve the same recognizable fill level and safe rim margin.
- [x] Test the computed surface bounds against the cup profile before using them in the renderer.
- [x] Do not add waves, droplets, foam geometry, bubbles, or motion cues.

### 3. Render coffee and tea inside the existing cup

- [x] Extend the existing CPU Skia renderer; keep the existing PNG encoding and rendering entry point.
- [x] Integrate the beverage surface into the existing cup `DrawMouth`/ceramic mouth stage, or split that stage into small explicit cavity/surface/rim helpers if needed for correct layering.
- [x] Preserve the current `DrawMouth` behavior byte-for-byte for empty cups wherever practical; an empty cup must not acquire liquid-only pixels or different material treatment.
- [x] Preserve bowl and vase mouth rendering exactly; only cups carrying typed beverage contents receive a liquid surface.
- [x] Draw the cavity gradient and inner ceramic shading first.
- [x] Draw the liquid surface after the cavity base but before the cup's final inner-rim, lip, and specular edge strokes.
- [x] Ensure the ceramic front lip and rim remain visibly in front of the liquid boundary; do not paint liquid across the rim.
- [x] Clip the liquid surface to the actual inner mouth/opening geometry, with antialiasing at the boundary.
- [x] Use a dark, warm, coffee-brown material with gentle depth shading and restrained amber/warm highlights.
- [x] Use a distinct tea-amber material with a lighter golden/copper body and a restrained darker perimeter/meniscus.
- [x] Make coffee and tea visibly distinguishable at normal viewing size without relying on labels or exaggerated saturation.
- [x] Use soft gradients and a small number of broad, controlled reflection/highlight shapes to suggest a still liquid surface.
- [x] Keep reflections consistent with the existing scene light direction.
- [x] Keep highlights subordinate to the liquid base; avoid a metallic, mirror-like, plastic, or glowing appearance.
- [x] Preserve enough of the cavity/interior near the rim to make the surface look recessed inside the cup.
- [x] Avoid a hard sticker-like ellipse edge; use a restrained meniscus or edge transition that sits naturally inside the ceramic opening.
- [x] Keep blue and cream glaze colors readable around the liquid without allowing the glaze shader to recolor the beverage.
- [x] Do not add steam, foam, latte art, bubbles, splashes, animated ripples, or other drink accessories.
- [x] Do not add an independent liquid cast/contact shadow on the wooden table; only the ceramic cup touches the table.
- [x] Preserve the cup's existing body gradient, glaze mottle, outer contour, handle ring, handle hole, rim thickness, foot, tabletop contact shadow, and cast-shadow direction.
- [x] Ensure the filled cup still reads correctly when the cup handle is on the existing side and the liquid surface does not spill into the handle hole.
- [x] Keep every temporary `SKPath`, shader, paint, mask filter, surface, and other native Skia resource correctly disposed.
- [x] Do not add static native-resource caches.
- [x] Ensure normal rendering, silhouette rendering, shadow-mask rendering, and detail-omission diagnostics all have explicit, consistent filled-cup behavior.
- [x] In silhouette output, preserve the existing cup silhouette and handle-hole convention; the liquid must not close the handle hole or expand the cup silhouette beyond its mouth/body.
- [x] In shadow-mask output, a filled cup must cast exactly the same tabletop shadow as the equivalent empty cup.
- [x] In detail-omission output, any optional liquid surface accents must follow the documented detail policy without suppressing the beverage's basic identity or changing later normal renders.
- [x] Ensure the fill surface is visually stable at 64–4096 dimensions and does not rely on a 1024-only pixel offset.

### 4. Extend the strict prompt grammar and generation/save pipeline

- [x] Add a specific typed `CupContentsPrompt`/parse result, or the smallest equivalent extension that leaves existing parser entry points compatible.
- [x] Parse the existing blue ceramic cup descriptor in the container position.
- [x] Parse the existing cream ceramic cup descriptor in the container position.
- [x] Parse exactly `coffee` and `tea` as beverage descriptors.
- [x] Support all four cup-glaze × beverage combinations.
- [x] Require the exact phrase `filled with` between the existing cup descriptor and beverage.
- [x] Require the existing terminal `on a wooden table` setting phrase.
- [x] Preserve the existing case, whitespace, and terminal-punctuation normalization behavior.
- [x] Preserve the semantic distinction between the cup and its beverage in the Core representation.
- [x] Route valid filled-cup prompts through the existing `SceneGenerator.Generate` and desktop generation service entry point.
- [x] Keep existing `Parse`, `ParseFruit`, `ParseScene`, `ParseObjects`, and Phase 5 `ParseContainment` behavior unchanged; document any necessary additive parser entry point.
- [x] Reject beverage prompts with bowls or vases.
- [x] Reject cup prompts with unsupported beverage words, missing `filled with`, reversed or ambiguous wording, and unsupported synonyms such as `contains`, `holding`, or `of`.
- [x] Reject fruit plus beverage, fruit inside a cup, cup plus another independent object, multiple cups, multiple beverages, quantities, plurals, extra adjectives, other colors, and malformed separators.
- [x] Reject malformed beverage prompts as a whole; never drop the drink and silently generate an empty cup.
- [x] Preserve all existing standalone fruit, fruit-group, ceramic, ceramic-group, mixed-scene, and fruit-in-bowl prompt behavior.
- [x] Preserve seed/variation snapshots, randomize behavior, background generation, duplicate-action guards, error handling, and previous-preview retention on failure.
- [x] Preserve last-successful-image metadata when prompt/seed/variation controls are edited without regenerating.
- [x] Add deterministic whitelisted subject tokens for coffee and tea in blue and cream cups; for example `coffee-in-blue-cup`, `coffee-in-cream-cup`, `tea-in-blue-cup`, and `tea-in-cream-cup`.
- [x] Add matching safe PNG filenames through the existing whitelist; never derive filenames from raw prompt text.
- [x] Preserve every existing fruit, group, ceramic, still-life, and fruit-in-bowl token/filename unchanged.
- [x] Update the concise prompt-help text with one filled-cup example; do not add desktop controls or panels.

### 5. Add Core, parser, and validation tests

- [x] Test all four supported cup/beverage/glaze prompt combinations.
- [x] Test case normalization, repeated whitespace, terminal punctuation, and existing exact-grammar behavior.
- [x] Test that the parsed cup kind, glaze, and beverage match the prompt exactly.
- [x] Test that the parse result explicitly represents the beverage as contents of a cup, not as an independent scene object.
- [x] Test that cup contents are mutually exclusive with `BowlContainment`.
- [x] Test Core validation for exactly one supported ceramic cup and one supported beverage.
- [x] Test rejection of a bowl or vase as a beverage container.
- [x] Test rejection of unsupported beverage values, missing cup, missing drink, multiple cups, multiple drinks, extra independent scene entries, and contradictory containment data.
- [x] Test rejection of `contains`, `holding`, `of`, reversed wording, plural/quantity forms, extra adjectives, unsupported colors, missing `ceramic`, unsupported drinks, malformed separators, and missing table phrase.
- [x] Test that an invalid filled-cup prompt is rejected as a whole and cannot fall back to an ordinary empty-cup prompt.
- [x] Test the liquid surface's normalized center and radii derived from the existing cup opening.
- [x] Test that the surface is finite, positive, bounded, and inset from every edge of the inner mouth ellipse by the defined safety margin.
- [x] Test that the surface bounds are independent of blue versus cream glaze.
- [x] Test that variation extremes preserve the same fill-level class and never make the drink touch/cover the rim.
- [x] Test deterministic surface geometry for identical inputs.
- [x] Retain all existing parser, scene-validation, ceramic-profile, bowl-containment, fruit-profile, and composition tests.

### 6. Add renderer pixel/invariant and desktop integration tests

- [x] Test valid PNG output for coffee and tea in both cup glazes at 1024 × 1024.
- [x] Test representative smaller, larger, square, and rectangular output dimensions already supported by the renderer.
- [x] Test identical filled-cup inputs produce identical PNG bytes/pixels on the same supported runtime.
- [x] Test alternate seeds produce coherent scene differences while preserving the requested cup, glaze, and beverage identity.
- [x] Add a non-vacuous pixel invariant proving the beverage surface is actually drawn inside the cup mouth.
- [x] Assert that a meaningful number of beverage-colored pixels differ from the equivalent empty-cup render.
- [x] Assert that coffee and tea produce visibly distinct liquid-region pixels under the same cup glaze and seed.
- [x] Assert that blue and cream glaze variants retain distinct ceramic pixels while the beverage identity remains unchanged.
- [x] Assert that all liquid pixels lie inside the cup's normalized inner opening with a safe margin from the outer rim.
- [x] Assert that liquid pixels do not leak onto the cup exterior, handle, handle hole, table, or background.
- [x] Assert that the ceramic rim/lip remains visible in front of the liquid edge and does not become covered by the fill.
- [x] Compare filled and empty versions of the same cup and assert that changed pixels are confined to the allowed mouth/liquid region; the body, handle, foot, and surrounding scene remain unchanged.
- [x] Assert that the filled cup's silhouette alpha mask is identical to the same empty cup's silhouette and that the handle hole remains transparent.
- [x] Assert that the filled cup's tabletop shadow mask is byte-identical to the same empty cup's shadow mask.
- [x] Assert that liquid rendering does not add a separate cast/contact shadow on the wooden table.
- [x] Assert that the cup's existing contact/cast shadow, light direction, handle outline, and canvas-edge safety are preserved.
- [x] Test that detail omission has the documented effect on beverage accents and does not alter pixels outside the cup's allowed interior region.
- [x] Test that diagnostic rendering followed by normal rendering produces the same normal-render bytes as a fresh render.
- [x] Retain all existing empty-cup, bowl, vase, ceramic-group, fruit-group, mixed-scene, fruit-in-bowl, clipping, shadow, silhouette, PNG, resource-ownership, and repeatability tests.
- [x] Extend generation-service/desktop-session tests to submit all four filled-cup combinations through the real prompt-to-PNG path.
- [x] Extend rendered-component tests to submit both coffee and tea prompts and verify successful generation metadata.
- [x] Test that an unsupported beverage prompt retains the previous successful preview and its matching save metadata.
- [x] Test all four new subject tokens/filenames and confirm every existing filename remains unchanged.

### 7. Generate and review visual/performance artifacts

- [x] Extend the existing `procedural-preview` test artifact; do not add a second workflow or artifact pipeline without a demonstrated need.
- [x] Generate default-seed/default-variation full-size PNGs for coffee-blue, coffee-cream, tea-blue, and tea-cream.
- [x] Generate at least one alternate-seed sample for each beverage.
- [x] Generate low- and high-variation examples for representative coffee and tea combinations.
- [x] Generate complete-cup silhouettes for representative filled cups and verify their expected equality to empty-cup silhouettes.
- [x] Generate a compact row-major review sheet and an accompanying manifest that identifies every cell's prompt, beverage, glaze, seed, variation, filename, and dimensions.
- [x] Include individual full-resolution 1024 × 1024 PNGs alongside the review sheet.
- [x] Keep generated PNGs and review composites in ignored test-output/artifact directories; do not commit generated binaries as source assets.
- [x] Generate timing sidecars for the four default cases and representative alternate/variation cases using the existing exporter conventions.
- [x] Measure render-plus-PNG-encode time at 1024 × 1024 for filled cups, and compare it with equivalent empty cups under the same runner/runtime; report observations, not a universal performance claim.
- [x] Inspect actual generated pixels where the working environment permits; producing or uploading a PNG alone is not visual review.
- [x] Review that the beverage is immediately identifiable as coffee or tea without a label.
- [x] Review that the surface reads as liquid recessed inside the cup, not a flat sticker, opaque lid, painted ceramic patch, or overflowing disk.
- [x] Review that the elliptical perspective agrees with the cup mouth and leaves a consistent visible ceramic rim/interior margin.
- [x] Review that the front lip and rim remain crisp and visible in front of the beverage edge.
- [x] Review both blue and cream glazes, alternate seeds, and variation extremes for leaks, clipping, strange highlights, or a changed handle.
- [x] Review that coffee is dark and warm while tea is distinctly amber/golden, without either becoming neon, metallic, or excessively saturated.
- [x] Review that reflection shapes are broad and restrained and do not resemble waves, foam, bubbles, or an animated surface.
- [x] Review that empty cups and all existing ceramic/fruit/containment examples remain visually unchanged.
- [x] If the agent cannot inspect the actual generated pixels, leave the human visual-review gate pending and provide exact artifact names, sheet dimensions/order, and manifest details; do not fabricate a visual pass.
- [x] Record cosmetic observations separately from defects and do not expand scope to fix unrelated pre-existing appearance preferences.

### 8. Documentation, verification, and handoff

- [x] Update the README current-capability section to mention still coffee/tea surfaces in ceramic cups.
- [x] Document the exact four supported filled-cup prompts and their strict grammar.
- [x] Document that only one coffee or tea surface in one blue/cream cup is supported in this milestone.
- [x] Document that empty cups remain supported and that bowls/vases cannot contain beverages.
- [x] Document that milk, foam, latte art, steam, pouring, splashing, other drinks, multiple contents, and arbitrary liquid wording remain unsupported.
- [x] Update the architecture overview only as much as needed to explain the explicit typed cup-contents value and cup-mouth rendering layer.
- [x] Add `docs/cup-liquids-handoff.md` with starting/ending commits, design decisions, typed Core model, surface geometry, mouth/rim draw order, grammar, subject tokens, tests, CI evidence, artifact names, timing observations, visual-review status, and known limitations.
- [x] Keep `.agents/todo-6.md` updated throughout implementation.
- [x] Mark a checkbox `[x]` only after its work is complete and the stated verification has actually been performed.
- [x] Leave blocked, unexecuted, or human-review tasks unchecked and explain the blocker/evidence beside them.
- [x] Run the established solution restore, Release warnings-as-errors build, and full solution test workflow through the existing CI gate.
- [x] Confirm all existing and new test projects pass on the latest PR CI run.
- [x] Confirm the post-merge Linux Release CI run passes before closing the milestone.
- [x] Keep ordinary CI Release-only; do not add a permanent Debug job, Windows runner, package, project, or workflow solely for this milestone.
- [x] Do not claim native Photino/WebView2 dialog, window-lifecycle, resize, or accessibility testing from headless tests or Linux CI.
- [x] Record exact PR, merge commit, and passing CI run URLs in the handoff.
- [x] Reconcile this checklist with the handoff and actual changed files; identify anything authored but not executed.
- [x] Stop at the acceptance checkpoint below and report implementation decisions, test/CI evidence, visual-review status, artifact location, timing observations, and known limitations.

## Acceptance checklist

- [x] All existing fruit, fruit-group, ceramic, ceramic-group, mixed-scene, and fruit-in-bowl prompts continue working unchanged.
- [x] Existing empty blue and cream cups render exactly as empty cups, with no liquid pixels or unintended visual changes.
- [x] All four coffee/tea × blue/cream cup prompts parse and generate through the existing desktop prompt-to-PNG path.
- [x] Core represents the beverage as typed contents of one cup, not as an independent scene object or generic relationship.
- [x] The beverage surface is derived from the cup's normalized mouth geometry and remains safely inside the rim.
- [x] Coffee and tea are recognizably distinct, still, recessed liquid surfaces at normal viewing size.
- [x] The cup's ceramic rim/lip remains visible above the liquid edge; no overflow, exterior leakage, or handle-hole corruption occurs.
- [x] The filled cup retains its original body, handle, silhouette, and tabletop shadow behavior.
- [x] Identical inputs are deterministic; seeds/variation do not break the fill or change the beverage/cup identity.
- [x] All Core/parser, renderer invariant, desktop integration, and existing regression tests pass.
- [x] Generated full-resolution samples and a correctly mapped review sheet are available; visual review is honestly recorded as passed or pending.
- [x] README and handoff accurately document supported prompts, architecture, evidence, and exclusions.
- [x] The completed change passes the repository's Linux Release CI gate and the post-merge run.
- [x] The agent stops and reports implementation decisions, actual test/CI evidence, sample artifacts, any visual observations, and known limitations.
- [x] Do not continue to another object family, beverage, or relationship milestone without review.

## Explicitly out of scope

- Milk, cream, hot chocolate, juice, soda, water, wine, beer, or any beverage beyond coffee and tea.
- Foam, crema art, latte art, bubbles, ice, lemon, tea bags, grounds, herbs, garnish, spoons, saucers, straws, or other props.
- Steam, pouring, splashing, sloshing, dynamic waves, animation, fluid simulation, physics, or 3D.
- Liquid in bowls, vases, or any non-cup vessel; fruit in cups; multiple beverages; mixed drinks; multiple cups; or additional scene objects in a filled-cup prompt.
- User-controlled fill levels, drink colors, reflections, material editors, camera controls, or new desktop UI panels.
- General-purpose liquid/material frameworks, generic containment/relationship systems, scene graphs, registries, plugin architectures, or arbitrary natural-language grammar.
- New packages, projects, frameworks, CI runners, or permanent workflow jobs.
- Unrelated refactors or visual redesigns of existing fruit, ceramic vessels, table, background, lighting, or composition.

## Working instruction

Implement this milestone directly in the existing repository, in small coherent steps, with tests as the system grows. Make reasonable engineering decisions without asking the user to design internal architecture. Do not stop after writing a plan or only documenting the work: complete the scoped implementation. Preserve the existing empty-cup rendering path as much as possible, and use the existing cup mouth profile and rim stages as the source of truth. If the current model exposes a genuine blocker, explain the evidence and choose the smallest compatible alternative rather than broadening the architecture. Keep the checklist accurate throughout, stop at the acceptance checkpoint, and provide the complete handoff.
