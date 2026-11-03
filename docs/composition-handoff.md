# Phase 3 — multi-fruit composition

## Status and starting point

Starting main commit: `36eaf70` (previous implementation baseline `a810f0f`, followed by the task-file addition). The working tree was clean before implementation, on the existing Arena session branch. No branch switching, dependency updates or new projects.

Implementation and tests below are **authored, not compiled/executed**. This sandbox has no .NET SDK; per project direction none is installed. No new PNGs have been generated or visually reviewed. Passing Phase 2 CI does not validate these edits. Final CI, post-merge verification and visual acceptance remain pending.

## Inspection and smallest extension / PR implementation note

The old path was strict `PromptParser.ParseFruit` → `SceneGenerator` → one `StillLifeScene.Apple` / `Subject` → `GenerationService` → `StillLifeRenderer.Render` → PNG/subject metadata → session/save. The scene's single body, singular subject token and renderer's one draw call were the one-subject assumptions. Existing geometry, material, body-space clipping, attachments and lighting already support an independently translated/scaled fruit.

- Extend existing Core parser/generator/scene; add only `FruitInstance`, `FruitBounds`, `FruitComposition`, `SceneValidator`.
- Extend the existing renderer top-level loop and shadow staging, leaving accepted contours/materials and attachment paths intact.
- Whitelist the generated `fruit-group` save token in the existing save service; retain single-fruit filenames.
- Extend existing component/service tests; add Core/composition and renderer tests in the existing projects. Existing artifact glob already collects the new PNGs and timing JSON.

## Core model and compatibility

`FruitInstance` carries typed subject, legacy body/appearance parameters, local random seed and depth. `StillLifeScene.Instances` is an optional immutable collection; `Fruits` exposes that collection or adapts the legacy single-fruit body. Explicit empty/default collections are invalid, distinct from a null compatibility collection. Prompt order is retained in the collection; rendering sorts a separate view by depth.

The original positional scene constructor, `Apple`, `Body`, `Subject`, single-fruit `ParseFruit` and apple-only `Parse` remain usable. The single-fruit generator keeps the original random stream names, body construction, placement and RNG consumption; `Instances` stays null, preserving existing single-scene record equality. Multi-scene record collection identity is not a sequence-equality API: compare `Fruits` elementwise for deterministic placements. Core remains free of Skia/desktop types.

Scene validation returns an actionable error string: instance count 1–3, supported appearance, finite/bounded transforms and attachment-aware margins. The generator returns expected errors as `GenerationResult`; the renderer rejects invalid direct scene inputs before raster allocation. Conservative complete bounds include stems/leaves, not just body rectangles. Legacy singles retain their previous smaller top margin; multi scenes use a 3% safe margin. A multi-instance projection with `Instances = null` is used only inside renderer/test helpers; setting/background are drawn from the original scene.

## Grammar

Exactly the six Phase 2 descriptors, with `on a wooden table` once at the end. One descriptor remains valid. Two use `and`. Three may use `and` throughout, `x, y and z`, or `x, y, and z`. Spaces around commas are normalized, along with existing case/whitespace/terminal punctuation. Repeats are allowed; no plurals, quantities, new adjectives, spatial prepositions, `or`, bare comma pairs, comma-only triples or lists beyond three. Invalid syntax is never partially consumed.

Examples:

- `a red apple and a green pear on a wooden table`
- `a green pear and an orange on a wooden table`
- `a red apple, a green pear, and an orange on a wooden table`
- `a yellow apple, a yellow pear and an orange on a wooden table`
- `an orange and an orange on a wooden table`

## Layout, ordering and randomness

Single: original centered placement, no composition jitter. Pair: two horizontal slots with common approximate base height. Trio: a smaller middle/back fruit and two slightly larger front fruits, with limited horizontal overlap. One `scene.composition` stream assigns prompt instances to slots using a seeded shuffle and restrained variation-scaled offsets. Composition seed changes can permute slots even at zero variation, without changing requested descriptors. Shapes and proportions are reused, not rewritten. Each body size follows its fruit's prior aspect ratio.

Local detail is not derived from slot/depth/order: first occurrences retain existing species streams seeded from the request. Repeated identical descriptors derive a stable extra seed from `instance.{kind}.{color}.{occurrence}`. Adding a different descriptor or reordering distinct descriptors preserves existing local shape, stem, leaf and surface randomness (but layout transforms necessarily change). Different color variants of the same species can share correlated first-occurrence local patterns; streams are independently constructed, not consumed from mutable shared RNG state. Repeat occurrence identity follows order among identical descriptors.

## Rendering and save metadata

Draw setting once. For groups, draw all per-instance ground shadows first, then bodies/attachments in stable back-to-front order. This prevents a later ground shadow being painted over an earlier fruit. Group cast-shadow blur/offset/height are scaled with body size and the oval is slightly narrower, keeping shadows proportionate and away from canvas edges. Single-fruit shadow constants and drawing sequence are retained. Every body uses the existing gradients, clipped detail, material, stem/leaf/calyx and transform path. Complete silhouettes draw the same ordered instances with no setting or shadows.

PNG metadata is the existing species token for singles and `fruit-group` for groups. Save names are `formloom-fruit-group-seed-42.png`, using a whitelist, not raw prompt text. The session still saves the previous successful image/seed after edited or failed prompts. No positional UI is introduced; help text describes the new list form.

## Authored verification and review artifacts

Core: valid/invalid lists, order/colors/count, 100 seeds × variation 0/0.35/1, bounded attachment-aware placement, spacing/overlap limits, ground heights, deterministic instances, seed-dependent slot assignments, repeated-fruit seed identity, stable details on adding/reordering, legacy single placement and invalid collections/transforms.

Renderer: PNG sizes 256/1024/1280 and 640×480, repeatability and seed differences, full instance/attachment-mask inclusion, edge transparency, body-center foreground visibility, clipped detail invariants and per-fruit contact darkness. Existing Phase 2 regression coverage remains. These are invariant tests, not proof of native GUI behavior or all visual intersections.

Desktop: existing real session→service→renderer→save-snapshot tests extended to pair/trio prompts, failure retention, seed snapshot and whitelisted filenames; rendered-component submission tests include pair/trio inputs.

Eleven new 1024px composition review cases: three pairs, two color trios, alternate seed 1337 for a pair/trio, and variation 0/1 for a pair/trio. PNGs use `formloom-group-*`; matching transparent complete silhouettes and advisory render/encode timing JSON include prompt/seed/variation and placements. Existing Phase 2 exporters supply single-fruit baselines. Files stay in ignored test output, covered by current CI artifact patterns.

## Pending gates and limits

- Full restore/Release warnings-as-errors build/test through the established single Linux job. No local runtime/compile/analyzer claim.
- Inspect actual group PNGs at normal size, including attachment/shadow overlap crops, low/high variation, seed permutations and single-fruit regressions.
- Assess three-fruit 1024px timing sidecars; do not infer performance from authored tests.
- Verify latest post-merge CI before declaring completion.
- No native-window/dialog tests are claimed. Same-runtime pixel determinism only, not cross-driver/platform guarantees. No new objects, props, GPU, grammar vocabulary, arbitrary scene graph or manual placement controls.

Stop at the Phase 3 acceptance checkpoint. All changes remain uncommitted until requested.


## Feasible static checks performed

`git diff --check`, solution/project XML parsing and local documentation-link checks passed. An independent scalar implementation of the seeded slot/size arithmetic checked 10,800 cases (apple/pear/orange combinations, two/three instances, seeds 0–99 and variation 0/0.35/1), confirming the chosen conservative margin, separation and overlap thresholds for those cases. This is a sanity check of the mathematics, not execution of the C# tests; duplicate-specific seed derivation, Skia rasterization, analyzer/build correctness and actual visual quality still require CI/review.
