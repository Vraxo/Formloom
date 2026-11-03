# Phase 3 — multi-fruit composition

## Status and starting point

Starting main commit: `36eaf70` (previous implementation baseline `a810f0f`, followed by the task-file addition). The working tree was clean before implementation, on the existing Arena session branch. No branch switching, dependency updates or new projects.

Implementation is merged in PR #22 at `f49b2be`. [Post-merge Linux Release run 41622641615](https://github.com/Vraxo/Formloom/actions/runs/41622641615) passed restore, warnings-as-errors build, tests and both artifact uploads. PR run 41622276993 passed at `5189444` after SA1116 formatting and unsupported-prompt wording fixes. The earlier failing test run reported 169 tests (168 passed, one error-wording assertion failure); the subsequent full test gate passed. No local SDK was installed.

CI generated group PNGs, complete silhouettes and timing sidecars, but **actual visual review and performance assessment remain pending**. The sandbox's redirected artifact-host access is restricted; user-supplied artifacts are needed. No native GUI execution is claimed.

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

## CI-executed verification and generated review artifacts

Core: valid/invalid lists, order/colors/count, 100 seeds × variation 0/0.35/1, bounded attachment-aware placement, spacing/overlap limits, ground heights, deterministic instances, seed-dependent slot assignments, repeated-fruit seed identity, stable details on adding/reordering, legacy single placement and invalid collections/transforms.

Renderer: PNG sizes 256/1024/1280 and 640×480, repeatability and seed differences, full instance/attachment-mask inclusion, edge transparency, body-center foreground visibility, clipped detail invariants and per-fruit contact darkness. Existing Phase 2 regression coverage remains. These are invariant tests, not proof of native GUI behavior or all visual intersections.

Desktop: existing real session→service→renderer→save-snapshot tests extended to pair/trio prompts, failure retention, seed snapshot and whitelisted filenames; rendered-component submission tests include pair/trio inputs.

Eleven new 1024px composition review cases: three pairs, two color trios, alternate seed 1337 for a pair/trio, and variation 0/1 for a pair/trio. PNGs use `formloom-group-*`; matching transparent complete silhouettes and advisory render/encode timing JSON include prompt/seed/variation and placements. Existing Phase 2 exporters supply single-fruit baselines. Files stay in ignored test output, covered by current CI artifact patterns.

## Pending gates and limits

- Full restore/Release warnings-as-errors build/test passed in the established single Linux job, not locally.
- Inspect actual group PNGs at normal size, including attachment/shadow overlap crops, low/high variation, seed permutations and single-fruit regressions.
- Assess three-fruit 1024px timing sidecars; do not infer performance from authored tests.
- Latest implementation post-merge CI is verified above; any further fixes need their own CI.
- No native-window/dialog tests are claimed. Same-runtime pixel determinism only, not cross-driver/platform guarantees. No new objects, props, GPU, grammar vocabulary, arbitrary scene graph or manual placement controls.

Stop at the Phase 3 acceptance checkpoint. Implementation is merged; current verification-note changes remain uncommitted.


## Feasible static checks performed

`git diff --check`, solution/project XML parsing and local documentation-link checks passed. An independent scalar implementation of the seeded slot/size arithmetic checked 10,800 cases (apple/pear/orange combinations, two/three instances, seeds 0–99 and variation 0/0.35/1), confirming the chosen conservative margin, separation and overlap thresholds for those cases. This is a sanity check of the mathematics, not execution of the C# tests; duplicate-specific seed derivation, Skia rasterization and analyzer/build correctness subsequently passed in CI; actual visual quality still requires image review.


## Review handoff after PR #22

Download `procedural-preview` from post-merge run 41622641615. Supply `formloom-group-*.png` (normal images and optional silhouettes), matching `formloom-group-*.timing.json`, and current single-fruit baseline PNGs. Group exports include three pair combinations, two trios, seed 1337 alternatives and variation 0/1 extremes. No claim of reviewed pixels or assessed group timings is made merely because artifact upload succeeded. Contact darkness and complete-subject border tests passed; cast-shadow intersections/extent and overlap-specific clipping remain explicit review/coverage gates.


## Actual group-image review — 2026-11-03 (partial artifact batch)

The user supplied 20 PNGs: ten normal group renders and ten transparent complete-group silhouettes. Normal renders cover apple/pear at seed 42 variations 0/0.35/1 and seed 1337 variation 0.35; orange/yellow-apple at seed 42 variation 0.35; red trios at seed 42 variations 0/0.35/1 and seed 1337 variation 0.35; and a yellow trio at seed 42 variation 0.35. The pear/orange pair is supplied as a silhouette only. No group timing JSON or current single-fruit baseline PNGs were supplied in this batch.

Normal images were visually inspected at their supplied 1024px size. All ten silhouettes were composited onto white for inspection only, with additional overview thumbnails; the all-black chat presentation was caused by black geometry on transparency, not evidence of an empty/failed image. Alpha bounding boxes for these supplied silhouettes are clear of all canvas edges.

### Findings for supplied images

- Pairs: the subjects have natural, readable spacing and plausible relative scale. Common lighting/table/shadows make the conservative side-by-side arrangement coherent; it is deliberately a simple still life, not a free-form composition engine. Pear/orange body silhouettes are distinct, but its color/shadow review is still pending the normal PNG.
- Trios: the smaller, higher middle fruit reads as a rear object, with larger outer fruits defining the foreground. All requested types remain recognizable. The apple/orange overlap is understandable and does not hide the apple indentation, stem or leaf. In a solid union silhouette, overlapping bodies merge locally as expected, while the two fruit profiles remain readable.
- Attachments: visible stems/leaves are connected and unclipped. Orange calyxes remain visible in color and merge into black body fill in silhouettes. An actual crop of the maximum-variation pair's apple stem/leaf junction was inspected with no apparent detachment.
- Grounding/lighting: consistent upper-left highlights and soft table shadows across supplied groups. Inspected a seed-42 trio apple/orange overlap-and-contact crop and a seed-1337 rear-orange/foreground-grounding crop. Rear fruit's higher contact position is consistent with its depth, not evidence of floating. No conspicuous detached contact gap or abruptly clipped required shadow was found in these samples. This is visual evidence, not a new automated cast-shadow extent test.
- Seed changes: the supplied trio changes from pear/apple/orange to apple/orange/pear, not merely a translated group. The pair's seed-1337 sample retains its left/right assignment and changes local shapes/transforms; not every pair of seeds must yield a different permutation.
- Variation 0/1: supplied apple/pear and red-trio extremes remain smooth, readable and inside the canvas, without overwhelming skin noise or hidden attachments.
- Surface detail at the inspected overlap has a clean silhouette boundary with no conspicuous spill. Visual inspection does not replace a targeted automated per-body overlap-clipping invariant; that coverage item stays open.

**Result:** no renderer fix is indicated by the supplied image set. This is a passing review of these supplied groups, not closure of all Phase 3 acceptance. Still required: the normal pear/orange pair PNG, current single-fruit family baselines, and all group timing sidecars. The alternate-seed trio silhouette was not attached, though its normal render was inspected. No native GUI or byte-identical historical regression comparison is claimed. Review composites/crops remain in ignored `artifacts/phase3-review`; no generated images are committed.


## Group CPU timing assessment — supplied batch, 2026-11-03

The user supplied eight of the eleven group timing reports, plus nine current single-fruit pear/orange reports (`timing-2.json`). Uploads are limited to 20 files per message; batches or a single ZIP can be used without resending previously reviewed files. This is a partial timing dataset, not a claim to have received all sidecars.

All supplied reports describe 1024×1024 output, .NET 10.0.14, Ubuntu 24.04.5 LTS and four logical processors. Stopwatch covers rendering and PNG encoding, excluding scene construction, file writes and desktop/GUI overhead.

| Composition | Supplied reports | Minimum (ms) | Median across settings (ms) | Maximum (ms) |
| --- | ---: | ---: | ---: | ---: |
| Two fruits | 3 | 187.61 | 386.23 | 503.62 |
| Three fruits | 5 | 175.39 | 233.11 | 291.43 |

| Group | Seed | Variation | Render + encode (ms) |
| --- | ---: | ---: | ---: |
| apple-pear | 1337 | 0.35 | 187.61 |
| apple-pear | 42 | 1.00 | 503.62 |
| orange-apple | 42 | 0.35 | 386.23 |
| red-trio | 1337 | 0.35 | 291.43 |
| red-trio | 42 | 0.00 | 219.38 |
| red-trio | 42 | 0.35 | 283.36 |
| red-trio | 42 | 1.00 | 233.11 |
| yellow-trio | 42 | 0.35 | 175.39 |

**Assessment:** the supplied three-fruit cases took 175–291 ms and the supplied pair cases 188–504 ms. These sub-second observations support suitable CPU rendering at the default size for the scoped still-image workflow. The maximum observed time is included, not hidden as an outlier. This does not establish a repeated-trial latency distribution, native Windows responsiveness, isolated scaling, peak-memory use or a cross-run regression verdict. Medians combine distinct prompts/settings; they are not repeated-workload benchmark statistics. The apparent pair/trio ranking is not evidence that adding a third fruit improves speed: workload size, JIT and concurrent/shared-runner activity are uncontrolled. Current single pear/orange reports span 193.81–331.08 ms, further cautioning against a direct comparison with Phase 2's earlier independent run.

Missing group timing reports are apple/pear seed 42 variations 0 and 0.35, and pear/orange seed 42 variation 0.35. They are not needed to establish that actual three-fruit cases were measured, but no values are inferred for them. The nine single-fruit timing files are metadata only; they do not replace the still-missing current baseline PNGs or the normal pear/orange pair PNG. Visual/overlap-coverage gates remain open independently of this performance assessment.


## Remaining normal images received and reviewed — 2026-11-03

The user supplied the normal pear/orange pair and current single-fruit PNGs (green pear seed 42 variation 0/0.35/1 and seed 1337 variation 0.35, yellow pear seed 42 variation 0.35, orange seed 42 variation 0/0.35/1 and seed 1337 variation 0.35, and both apple baseline exports). Actual inspection completes the requested normal-image review across all eleven exported group cases and the single-fruit family baselines.

The pear/orange pair has readable separation, coherent upper-left lighting, restrained skin detail and attached/unclipped pear stem/leaf. Both bodies meet their table contact shadows convincingly for the established stylized rendering. The current singles retain the reviewed apple lobes/polish, pear neck/belly and matte skin, and orange round/asymmetric form and subtle peel. Variation extremes remain smooth. No visible regression or renderer fix is indicated; this is visual inspection, not a historical byte-for-byte comparison.

No further user image uploads are needed for the current visual acceptance gate. The previously documented missing normal PNGs have now been received; earlier partial-batch notes describe their historical status. Eight group timing reports are already sufficient for the stated advisory assessment; the three unreceived reports are not being requested merely for completeness. Automated overlap-specific clipping, cast-shadow extent and resource-ownership/checklist reconciliation remain separate from visual approval.

The user reports a 20-file upload limit and ZIP uploads are unsupported in this interface; the earlier ZIP suggestion was incorrect for this environment. Repeated manual artifact transfer is a tooling constraint, not a required test ritual. Future workflow simplification should retain automated coverage but consolidate reviewer-facing output and reserve visual review for meaningful visual changes/milestone gates, not documentation or unchanged rendering. No new workflow policy or preview-generation code has been implemented by this documentation update.
