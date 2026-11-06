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

- [ ] Preserve the existing stack, target framework, package versions, solution/project boundaries, analyzer policy, and CI workflow unless a concrete blocker justifies a small compatible change.
- [ ] Keep generation offline, CPU-first, non-neural, seeded, deterministic, and inspectable.
- [ ] Keep Core independent of Skia, pixels, bitmaps, native resources, and desktop/UI types.
- [ ] Reuse the existing prompt → Core → generation service → Skia → PNG/save path.
- [ ] Preserve every existing supported prompt, parser contract, image path, deterministic stream, subject token, and save filename.
- [ ] Preserve the accepted visual appearance of empty cups, bowls, vases, fruit, mixed scenes, and Phase 5 fruit-in-bowl scenes.
- [ ] Keep the existing desktop controls; do not add manual fill-level, liquid-color, reflection, or material controls.
- [ ] Add no downloaded images, stock textures, fonts, network dependencies, models, packages, projects, frameworks, or CI runners.
- [ ] Do not introduce a general-purpose liquid/material system, fluid solver, physics engine, scene graph, arbitrary relationship engine, or 3D renderer.
- [ ] Keep this milestone limited to one still coffee or tea surface inside one existing cup on the existing wooden table.
- [ ] Use the current cup mouth geometry as the source of truth; do not hard-code canvas-pixel positions for the liquid.
- [ ] Treat the drink as a contained surface/material associated with the cup, not as an independent scene object that casts a tabletop shadow or participates in ordinary depth sorting.

## Sequential implementation checklist

### 0. Inspect and record the actual starting point

- [ ] Start from the latest `main`; record the exact starting commit and confirm the working tree is clean.
- [ ] Read `.agents/todo-5.md` and `docs/containment-handoff.md` in full.
- [ ] Inspect the current `StillLifeScene`, `CeramicInstance`, `SceneEntry`, scene validation, parser, generator, subject-token construction, and save-filename whitelist.
- [ ] Trace an empty-cup prompt end-to-end through parsing, Core construction, renderer, generation service, desktop session, preview metadata, and PNG saving.
- [ ] Inspect `CeramicProfile` and the cup profile's `MouthCenter`, `MouthRadius`, `CavityDepth`, `RimThickness`, outline and handle geometry.
- [ ] Read `StillLifeRenderer.DrawCeramic` and `DrawMouth` in full, including the outline clip, cavity gradient, inner shading, ellipse/rim strokes, handle rendering, and all save/restore boundaries.
- [ ] Inspect `RenderSilhouette`, `RenderShadowMask`, and `RenderWithoutInstanceDetail` to understand how a filled cup should behave in diagnostic passes.
- [ ] Inspect existing ceramic pixel tests, silhouette/handle-hole tests, desktop generation tests, component tests, PNG filename tests, and the `procedural-preview` exporter.
- [ ] Record which existing mouth-rendering operations can be reused without changing empty-cup output and where the beverage surface must be inserted relative to the cavity and rim.
- [ ] Write a short implementation note in the PR description naming the smallest Core, parser, renderer, desktop, test, and documentation changes.
- [ ] Choose a small explicit typed cup-content representation; do not begin with a generic container/content abstraction.

### 1. Model cup contents explicitly in Core

- [ ] Add a small closed beverage type with exactly `Coffee` and `Tea` values.
- [ ] Add an explicit typed cup-contents value that associates one existing `CeramicInstance` of kind `Cup` with one beverage kind.
- [ ] Keep the beverage value in Core free of Skia colors, paints, shaders, pixels, and rendering resources.
- [ ] Keep the existing `CeramicInstance` and `SceneEntry` representations intact for ordinary empty vessels and mixed scenes.
- [ ] Add one narrowly scoped optional cup-contents property to `StillLifeScene`, or an equally small typed representation that fits the current scene model.
- [ ] Keep the cup itself as the scene's one ceramic entry; do not add the liquid as a second independently positioned scene object.
- [ ] Ensure the typed contents value refers to the exact cup represented by the scene, with no duplicated or contradictory cup data.
- [ ] Preserve the cup's existing kind, glaze, body transform, seed, depth, profile and handle.
- [ ] Keep cup contents mutually exclusive with Phase 5 `BowlContainment`; a scene cannot be both a filled cup and a fruit-in-bowl containment scene.
- [ ] Validate that the container is exactly one supported ceramic cup with blue or cream glaze.
- [ ] Validate that the beverage is exactly coffee or tea.
- [ ] Reject a missing cup, non-cup vessel, missing beverage, invalid beverage value, duplicate cup, extra independent scene entries, and contradictory containment state.
- [ ] Keep existing empty-vessel and one-to-three independent-object scene rules unchanged.
- [ ] Keep validation errors explicit and actionable; never silently discard the beverage or fall back to an empty cup.
- [ ] Derive the liquid's normalized surface geometry from the existing cup profile's opening ellipse and rim thickness, not from arbitrary screen coordinates.
- [ ] Keep geometry and beverage appearance separate: Core describes the beverage identity and, if needed, normalized surface bounds; Skia owns the rendered palette and shading.
- [ ] Avoid a generalized liquid framework, material registry, type-name strings, reflection, or an `object` field.

### 2. Define a restrained, profile-aware liquid surface

- [ ] Calculate the liquid surface from `CeramicProfile.MouthCenter` and `MouthRadius` for the cup.
- [ ] Keep the surface inside the usable inner mouth ellipse with a visible ceramic rim/interior margin on every side.
- [ ] Account for `RimThickness` so the liquid does not cover the ceramic lip or appear painted over the rim.
- [ ] Use a flattened ellipse consistent with the existing cup opening perspective; do not draw a circular disk or a horizontal rectangle.
- [ ] Place the liquid slightly below the top lip so the cup reads as filled but not overflowing.
- [ ] Keep the surface bounds finite, normalized, positive, and safely inside the opening across all supported output sizes.
- [ ] Use one fixed, believable fill level for both drinks in this milestone; no prompt-controlled or UI-controlled volume.
- [ ] Keep the amount of visible inner wall consistent with the cup's existing cavity depth.
- [ ] Avoid exposing a large empty cavity behind the surface or filling the entire opening edge-to-edge.
- [ ] Make the surface geometry independent of cup glaze so switching blue ↔ cream does not shift or resize the liquid.
- [ ] If any seeded surface irregularity is genuinely needed, put it on a dedicated named stream and bound it tightly; a static deterministic surface is preferred over decorative randomness.
- [ ] Ensure variation 0 and variation 1 both preserve the same recognizable fill level and safe rim margin.
- [ ] Test the computed surface bounds against the cup profile before using them in the renderer.
- [ ] Do not add waves, droplets, foam geometry, bubbles, or motion cues.

### 3. Render coffee and tea inside the existing cup

- [ ] Extend the existing CPU Skia renderer; keep the existing PNG encoding and rendering entry point.
- [ ] Integrate the beverage surface into the existing cup `DrawMouth`/ceramic mouth stage, or split that stage into small explicit cavity/surface/rim helpers if needed for correct layering.
- [ ] Preserve the current `DrawMouth` behavior byte-for-byte for empty cups wherever practical; an empty cup must not acquire liquid-only pixels or different material treatment.
- [ ] Preserve bowl and vase mouth rendering exactly; only cups carrying typed beverage contents receive a liquid surface.
- [ ] Draw the cavity gradient and inner ceramic shading first.
- [ ] Draw the liquid surface after the cavity base but before the cup's final inner-rim, lip, and specular edge strokes.
- [ ] Ensure the ceramic front lip and rim remain visibly in front of the liquid boundary; do not paint liquid across the rim.
- [ ] Clip the liquid surface to the actual inner mouth/opening geometry, with antialiasing at the boundary.
- [ ] Use a dark, warm, coffee-brown material with gentle depth shading and restrained amber/warm highlights.
- [ ] Use a distinct tea-amber material with a lighter golden/copper body and a restrained darker perimeter/meniscus.
- [ ] Make coffee and tea visibly distinguishable at normal viewing size without relying on labels or exaggerated saturation.
- [ ] Use soft gradients and a small number of broad, controlled reflection/highlight shapes to suggest a still liquid surface.
- [ ] Keep reflections consistent with the existing scene light direction.
- [ ] Keep highlights subordinate to the liquid base; avoid a metallic, mirror-like, plastic, or glowing appearance.
- [ ] Preserve enough of the cavity/interior near the rim to make the surface look recessed inside the cup.
- [ ] Avoid a hard sticker-like ellipse edge; use a restrained meniscus or edge transition that sits naturally inside the ceramic opening.
- [ ] Keep blue and cream glaze colors readable around the liquid without allowing the glaze shader to recolor the beverage.
- [ ] Do not add steam, foam, latte art, bubbles, splashes, animated ripples, or other drink accessories.
- [ ] Do not add an independent liquid cast/contact shadow on the wooden table; only the ceramic cup touches the table.
- [ ] Preserve the cup's existing body gradient, glaze mottle, outer contour, handle ring, handle hole, rim thickness, foot, tabletop contact shadow, and cast-shadow direction.
- [ ] Ensure the filled cup still reads correctly when the cup handle is on the existing side and the liquid surface does not spill into the handle hole.
- [ ] Keep every temporary `SKPath`, shader, paint, mask filter, surface, and other native Skia resource correctly disposed.
- [ ] Do not add static native-resource caches.
- [ ] Ensure normal rendering, silhouette rendering, shadow-mask rendering, and detail-omission diagnostics all have explicit, consistent filled-cup behavior.
- [ ] In silhouette output, preserve the existing cup silhouette and handle-hole convention; the liquid must not close the handle hole or expand the cup silhouette beyond its mouth/body.
- [ ] In shadow-mask output, a filled cup must cast exactly the same tabletop shadow as the equivalent empty cup.
- [ ] In detail-omission output, any optional liquid surface accents must follow the documented detail policy without suppressing the beverage's basic identity or changing later normal renders.
- [ ] Ensure the fill surface is visually stable at 64–4096 dimensions and does not rely on a 1024-only pixel offset.

### 4. Extend the strict prompt grammar and generation/save pipeline

- [ ] Add a specific typed `CupContentsPrompt`/parse result, or the smallest equivalent extension that leaves existing parser entry points compatible.
- [ ] Parse the existing blue ceramic cup descriptor in the container position.
- [ ] Parse the existing cream ceramic cup descriptor in the container position.
- [ ] Parse exactly `coffee` and `tea` as beverage descriptors.
- [ ] Support all four cup-glaze × beverage combinations.
- [ ] Require the exact phrase `filled with` between the existing cup descriptor and beverage.
- [ ] Require the existing terminal `on a wooden table` setting phrase.
- [ ] Preserve the existing case, whitespace, and terminal-punctuation normalization behavior.
- [ ] Preserve the semantic distinction between the cup and its beverage in the Core representation.
- [ ] Route valid filled-cup prompts through the existing `SceneGenerator.Generate` and desktop generation service entry point.
- [ ] Keep existing `Parse`, `ParseFruit`, `ParseScene`, `ParseObjects`, and Phase 5 `ParseContainment` behavior unchanged; document any necessary additive parser entry point.
- [ ] Reject beverage prompts with bowls or vases.
- [ ] Reject cup prompts with unsupported beverage words, missing `filled with`, reversed or ambiguous wording, and unsupported synonyms such as `contains`, `holding`, or `of`.
- [ ] Reject fruit plus beverage, fruit inside a cup, cup plus another independent object, multiple cups, multiple beverages, quantities, plurals, extra adjectives, other colors, and malformed separators.
- [ ] Reject malformed beverage prompts as a whole; never drop the drink and silently generate an empty cup.
- [ ] Preserve all existing standalone fruit, fruit-group, ceramic, ceramic-group, mixed-scene, and fruit-in-bowl prompt behavior.
- [ ] Preserve seed/variation snapshots, randomize behavior, background generation, duplicate-action guards, error handling, and previous-preview retention on failure.
- [ ] Preserve last-successful-image metadata when prompt/seed/variation controls are edited without regenerating.
- [ ] Add deterministic whitelisted subject tokens for coffee and tea in blue and cream cups; for example `coffee-in-blue-cup`, `coffee-in-cream-cup`, `tea-in-blue-cup`, and `tea-in-cream-cup`.
- [ ] Add matching safe PNG filenames through the existing whitelist; never derive filenames from raw prompt text.
- [ ] Preserve every existing fruit, group, ceramic, still-life, and fruit-in-bowl token/filename unchanged.
- [ ] Update the concise prompt-help text with one filled-cup example; do not add desktop controls or panels.

### 5. Add Core, parser, and validation tests

- [ ] Test all four supported cup/beverage/glaze prompt combinations.
- [ ] Test case normalization, repeated whitespace, terminal punctuation, and existing exact-grammar behavior.
- [ ] Test that the parsed cup kind, glaze, and beverage match the prompt exactly.
- [ ] Test that the parse result explicitly represents the beverage as contents of a cup, not as an independent scene object.
- [ ] Test that cup contents are mutually exclusive with `BowlContainment`.
- [ ] Test Core validation for exactly one supported ceramic cup and one supported beverage.
- [ ] Test rejection of a bowl or vase as a beverage container.
- [ ] Test rejection of unsupported beverage values, missing cup, missing drink, multiple cups, multiple drinks, extra independent scene entries, and contradictory containment data.
- [ ] Test rejection of `contains`, `holding`, `of`, reversed wording, plural/quantity forms, extra adjectives, unsupported colors, missing `ceramic`, unsupported drinks, malformed separators, and missing table phrase.
- [ ] Test that an invalid filled-cup prompt is rejected as a whole and cannot fall back to an ordinary empty-cup prompt.
- [ ] Test the liquid surface's normalized center and radii derived from the existing cup opening.
- [ ] Test that the surface is finite, positive, bounded, and inset from every edge of the inner mouth ellipse by the defined safety margin.
- [ ] Test that the surface bounds are independent of blue versus cream glaze.
- [ ] Test that variation extremes preserve the same fill-level class and never make the drink touch/cover the rim.
- [ ] Test deterministic surface geometry for identical inputs.
- [ ] Retain all existing parser, scene-validation, ceramic-profile, bowl-containment, fruit-profile, and composition tests.

### 6. Add renderer pixel/invariant and desktop integration tests

- [ ] Test valid PNG output for coffee and tea in both cup glazes at 1024 × 1024.
- [ ] Test representative smaller, larger, square, and rectangular output dimensions already supported by the renderer.
- [ ] Test identical filled-cup inputs produce identical PNG bytes/pixels on the same supported runtime.
- [ ] Test alternate seeds produce coherent scene differences while preserving the requested cup, glaze, and beverage identity.
- [ ] Add a non-vacuous pixel invariant proving the beverage surface is actually drawn inside the cup mouth.
- [ ] Assert that a meaningful number of beverage-colored pixels differ from the equivalent empty-cup render.
- [ ] Assert that coffee and tea produce visibly distinct liquid-region pixels under the same cup glaze and seed.
- [ ] Assert that blue and cream glaze variants retain distinct ceramic pixels while the beverage identity remains unchanged.
- [ ] Assert that all liquid pixels lie inside the cup's normalized inner opening with a safe margin from the outer rim.
- [ ] Assert that liquid pixels do not leak onto the cup exterior, handle, handle hole, table, or background.
- [ ] Assert that the ceramic rim/lip remains visible in front of the liquid edge and does not become covered by the fill.
- [ ] Compare filled and empty versions of the same cup and assert that changed pixels are confined to the allowed mouth/liquid region; the body, handle, foot, and surrounding scene remain unchanged.
- [ ] Assert that the filled cup's silhouette alpha mask is identical to the same empty cup's silhouette and that the handle hole remains transparent.
- [ ] Assert that the filled cup's tabletop shadow mask is byte-identical to the same empty cup's shadow mask.
- [ ] Assert that liquid rendering does not add a separate cast/contact shadow on the wooden table.
- [ ] Assert that the cup's existing contact/cast shadow, light direction, handle outline, and canvas-edge safety are preserved.
- [ ] Test that detail omission has the documented effect on beverage accents and does not alter pixels outside the cup's allowed interior region.
- [ ] Test that diagnostic rendering followed by normal rendering produces the same normal-render bytes as a fresh render.
- [ ] Retain all existing empty-cup, bowl, vase, ceramic-group, fruit-group, mixed-scene, fruit-in-bowl, clipping, shadow, silhouette, PNG, resource-ownership, and repeatability tests.
- [ ] Extend generation-service/desktop-session tests to submit all four filled-cup combinations through the real prompt-to-PNG path.
- [ ] Extend rendered-component tests to submit both coffee and tea prompts and verify successful generation metadata.
- [ ] Test that an unsupported beverage prompt retains the previous successful preview and its matching save metadata.
- [ ] Test all four new subject tokens/filenames and confirm every existing filename remains unchanged.

### 7. Generate and review visual/performance artifacts

- [ ] Extend the existing `procedural-preview` test artifact; do not add a second workflow or artifact pipeline without a demonstrated need.
- [ ] Generate default-seed/default-variation full-size PNGs for coffee-blue, coffee-cream, tea-blue, and tea-cream.
- [ ] Generate at least one alternate-seed sample for each beverage.
- [ ] Generate low- and high-variation examples for representative coffee and tea combinations.
- [ ] Generate complete-cup silhouettes for representative filled cups and verify their expected equality to empty-cup silhouettes.
- [ ] Generate a compact row-major review sheet and an accompanying manifest that identifies every cell's prompt, beverage, glaze, seed, variation, filename, and dimensions.
- [ ] Include individual full-resolution 1024 × 1024 PNGs alongside the review sheet.
- [ ] Keep generated PNGs and review composites in ignored test-output/artifact directories; do not commit generated binaries as source assets.
- [ ] Generate timing sidecars for the four default cases and representative alternate/variation cases using the existing exporter conventions.
- [ ] Measure render-plus-PNG-encode time at 1024 × 1024 for filled cups, and compare it with equivalent empty cups under the same runner/runtime; report observations, not a universal performance claim.
- [ ] Inspect actual generated pixels where the working environment permits; producing or uploading a PNG alone is not visual review.
- [ ] Review that the beverage is immediately identifiable as coffee or tea without a label.
- [ ] Review that the surface reads as liquid recessed inside the cup, not a flat sticker, opaque lid, painted ceramic patch, or overflowing disk.
- [ ] Review that the elliptical perspective agrees with the cup mouth and leaves a consistent visible ceramic rim/interior margin.
- [ ] Review that the front lip and rim remain crisp and visible in front of the beverage edge.
- [ ] Review both blue and cream glazes, alternate seeds, and variation extremes for leaks, clipping, strange highlights, or a changed handle.
- [ ] Review that coffee is dark and warm while tea is distinctly amber/golden, without either becoming neon, metallic, or excessively saturated.
- [ ] Review that reflection shapes are broad and restrained and do not resemble waves, foam, bubbles, or an animated surface.
- [ ] Review that empty cups and all existing ceramic/fruit/containment examples remain visually unchanged.
- [ ] If the agent cannot inspect the actual generated pixels, leave the human visual-review gate pending and provide exact artifact names, sheet dimensions/order, and manifest details; do not fabricate a visual pass.
- [ ] Record cosmetic observations separately from defects and do not expand scope to fix unrelated pre-existing appearance preferences.

### 8. Documentation, verification, and handoff

- [ ] Update the README current-capability section to mention still coffee/tea surfaces in ceramic cups.
- [ ] Document the exact four supported filled-cup prompts and their strict grammar.
- [ ] Document that only one coffee or tea surface in one blue/cream cup is supported in this milestone.
- [ ] Document that empty cups remain supported and that bowls/vases cannot contain beverages.
- [ ] Document that milk, foam, latte art, steam, pouring, splashing, other drinks, multiple contents, and arbitrary liquid wording remain unsupported.
- [ ] Update the architecture overview only as much as needed to explain the explicit typed cup-contents value and cup-mouth rendering layer.
- [ ] Add `docs/cup-liquids-handoff.md` with starting/ending commits, design decisions, typed Core model, surface geometry, mouth/rim draw order, grammar, subject tokens, tests, CI evidence, artifact names, timing observations, visual-review status, and known limitations.
- [ ] Keep `.agents/todo-6.md` updated throughout implementation.
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

- [ ] All existing fruit, fruit-group, ceramic, ceramic-group, mixed-scene, and fruit-in-bowl prompts continue working unchanged.
- [ ] Existing empty blue and cream cups render exactly as empty cups, with no liquid pixels or unintended visual changes.
- [ ] All four coffee/tea × blue/cream cup prompts parse and generate through the existing desktop prompt-to-PNG path.
- [ ] Core represents the beverage as typed contents of one cup, not as an independent scene object or generic relationship.
- [ ] The beverage surface is derived from the cup's normalized mouth geometry and remains safely inside the rim.
- [ ] Coffee and tea are recognizably distinct, still, recessed liquid surfaces at normal viewing size.
- [ ] The cup's ceramic rim/lip remains visible above the liquid edge; no overflow, exterior leakage, or handle-hole corruption occurs.
- [ ] The filled cup retains its original body, handle, silhouette, and tabletop shadow behavior.
- [ ] Identical inputs are deterministic; seeds/variation do not break the fill or change the beverage/cup identity.
- [ ] All Core/parser, renderer invariant, desktop integration, and existing regression tests pass.
- [ ] Generated full-resolution samples and a correctly mapped review sheet are available; visual review is honestly recorded as passed or pending.
- [ ] README and handoff accurately document supported prompts, architecture, evidence, and exclusions.
- [ ] The completed change passes the repository's Linux Release CI gate and the post-merge run.
- [ ] The agent stops and reports implementation decisions, actual test/CI evidence, sample artifacts, any visual observations, and known limitations.
- [ ] Do not continue to another object family, beverage, or relationship milestone without review.

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
