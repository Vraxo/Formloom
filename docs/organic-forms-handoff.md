# Organic forms milestone — implementation and verification notes

## Current status

Implementation is merged in PR #16. [Post-merge Linux Release CI run 41584177007](https://github.com/Vraxo/Formloom/actions/runs/41584177007) passed restore, warnings-as-errors build, both test projects and both artifact uploads at `d6aa8ff`. PR run 41584029986 also passed; its reported test total was 115 passed, zero failed/skipped. The user subsequently confirmed a successful Windows Release restore/build with the inherited NuGet-source conflict resolved. These are not native GUI smoke-test results.

**Actual visual review performed on 2026-11-03:** the user supplied all 14 exported PNGs after direct artifact download failed in the sandbox. The review passes for the scoped stylized renderer, with the limitations below. No native GUI execution or pixel-perfect comparison with historical accepted images is claimed.

The new task file was fetched from main and fast-forwarded onto the session branch. The checkout initially had shallow history; fetching that history resolved the fast-forward without changing branches or merging unrelated histories.

## Inspection and smallest extension

Existing reusable pieces: `StillLifeRenderer`'s setting, transforms, gradients, clipped detail, contact/cast shadows and stem/leaf helpers; `WoodGrain`; `Pcg32.ForStream`; strict parser normalization; `GenerationService`, `DesktopSession`, PNG writer and existing test projects. Apple-specific pieces were the five body Bezier spans, apple color palette, polish/speckles and the assumed stem origin.

Files extended: `PromptParser`, `SceneGenerator`, `StillLifeScene`, renderer, encoded-image metadata, native save naming, existing desktop/component tests and CI artifact paths. New Core files: fruit enums/prompt, `CubicSegment`, `OrganicContour`, `OrganicForms`. New renderer file: `FruitMaterial`. Tests grow in the existing projects; no new projects, packages, frameworks or runners.

## Shared construction

Core describes smooth closed contours as immutable cubic segments with normalized vectors and an explicit top attachment. A shared periodic Catmull–Rom-to-Bezier construction builds pear/orange contours from sampled semantic radial profiles. Pear width grows from a narrow upper neck to a broad lower belly using smoothstep taper; orange has low-amplitude natural irregularity even at zero variation. Named `pear.contour` / `orange.contour` streams introduce bounded deformation, not independent per-vertex noise.

The accepted apple's exact five Bezier spans are preserved in a Core profile factory rather than approximated by the new radial profile. All three contours pass through the same path conversion and drawing stages; there are not three independent renderers. Retaining the existing apple profile is intentional preservation of reviewed geometry, not a universal vector editor.

Compatibility: `PromptParser.Parse` still returns the original apple-only color result; `ParseFruit` is the explicit all-fruit parser. `StillLifeScene.Apple` / `AppleDescription` remain the legacy body-parameter storage for existing callers/tests, with `Body`, typed `Subject` and semantic `Color` exposing the shared interpretation. Orange's actual material is orange, not the legacy yellow storage value. This narrow compatibility layer avoids breaking the existing public constructor/tests; it is not intended as a multi-object scene graph.

## Materials and attachments

`FruitMaterial` separates palette, highlight and detail settings from geometry. Shared renderer stages draw the contour, coherent directional lighting, clipped surface detail and shadows. Pears use softer, more matte highlights; oranges use restrained dark/light micro-pores, not bright random dots. `SurfaceDetail` is a scene-level appearance multiplier for invariant tests, not a new desktop control. Details remain clipped to the contour.

Stem/leaf drawing is reused for apple/pear, translated from the profile's attachment. Orange uses a small calyx instead of the same long stem and leaf. Wood, background and their streams are unchanged. Apple streams keep their original names and consumption; new fruits use independent subject-prefixed streams.

## Exact prompt forms

- `a red apple on a wooden table`
- `a green apple on a wooden table`
- `a yellow apple on a wooden table`
- `a green pear on a wooden table`
- `a yellow pear on a wooden table`
- `an orange on a wooden table`

Case/whitespace/terminal punctuation normalization is unchanged. Other articles, adjectives, colors, subjects, materials and relations fail explicitly. The existing prompt UI/service/renderer entry point is retained; no composition controls are added.

The rendered-image result carries the successful subject name. The save service uses a whitelisted subject token for filenames and the existing saved-image seed. Thus an ungenerated prompt edit does not relabel a previously generated fruit, and existing apple filenames remain unchanged.

## Passing automated coverage and actual visual review

- Parsing to explicit subject/color and unsupported combinations.
- Sampled cubic continuity/closure, finiteness/bounds, deterministic segments, meaningful pear neck/belly ratio, profile differences and stream isolation.
- Valid PNGs across 256/1024/1280 and rectangular sizes, variation extremes, seed changes and pixel repeatability.
- Black silhouette distinction and exports for human recognition review.
- Orange detail-on/off comparisons prove outside-contour background/table pixels are unchanged.
- Real desktop pipeline/save metadata tests for new prompts and failure retention, plus actual component input/submission coverage. Existing opacity, grounding, grain and session/save tests remain in place and passed in Release CI.

CI's existing **procedural-preview** artifact now collects `formloom-*.png` from the original renderer test output directory, including apple, green/yellow pear and orange samples, alternate seeds for the new fruits, low/high variation and silhouettes. Files are generated by test execution, not embedded artwork, and remain under ignored `artifacts/`. CI generated and uploaded these artifacts successfully; the user subsequently supplied all 14 PNGs and their pixels were visually reviewed.

Restore/build/test commands and CI policy are unchanged. No permanent Debug job or additional runner is introduced. Before declaring this milestone complete, inspect actual samples at normal size, verify pear silhouette/orange peel and apple preservation, and record a passing latest Release CI run. Native UI tests are not claimed by headless tests. Stop at this milestone; no additional fruit family or feature is authorized.


## Visual findings — supplied CI samples

- Apple: recognizable lobes/top indentation, attached stem/leaf, broad polished highlight and restrained speckles are retained. The two supplied apple renders look consistent; this is visual inspection, not proof of historical byte identity.
- Pears: unmistakable narrow neck and broad belly, also recognizable in the black silhouette. Green and yellow are distinct; soft highlights and restrained speckling read more matte than the apple. Stems/leaves remain attached across the supplied variations.
- Orange: rounded but slightly asymmetric profile, warm lit-to-shadow volume, subtle pores and small calyx read as a stylized orange. Texture does not become polka dots or glitter. The peel is intentionally understated and the calyx is a simplified flat mark, not photorealistic detail.
- Seed/variation samples: all supplied forms remain smooth and recognizable, with controlled changes in size/asymmetry and attachment placement. No visible breakdown at either variation extreme. Filenames identify seed/variation; this review does not infer settings from pixels alone.
- Setting: consistent upper-left lighting, soft cast/contact shadows and unchanged wooden-table/background styling. Round fruit has a small contact footprint but no obvious detached gap in these samples.
- Silhouettes: black-on-transparent PNGs appeared black in the chat viewer. White compositing for inspection revealed three distinct profiles. The orange silhouette export touches the right canvas edge: a presentation-margin limitation of the unplaced mask export, not clipping of the normal still-life render. Future silhouette presentation can add padding without changing fruit geometry.

Remaining evidence boundaries: no new native desktop smoke test, no dedicated CPU benchmark, no historical golden-image comparison. Existing headless desktop coverage and full Release CI passed. No additional subject family or feature is started. This documentation update does not change renderer behavior.


## Review clarifications (before the silhouette follow-up)

The current black silhouette exports are **body-only contour masks**: they intentionally omit stems/leaves and the orange calyx. They verify body-profile recognition, not the outline of the complete rendered subject. Complete-subject silhouette exports, reusing the renderer's attachment geometry with sufficient padding, were discussed but are not implemented by this documentation change. Body-only masks remain useful for contour/peel-clipping tests.

With seed 42 held constant, changing fruit variation leaves the table/background fixed. This is intentional: named setting streams are independent of the fruit's geometry/detail streams. Changing the seed can change the wood grain as well. No control was renamed in this update.

## Complete-subject silhouette follow-up (CI and visual review verified)

Added `StillLifeRenderer.RenderSilhouette` through the same body/stem/leaf/calyx drawing path as normal rendering. It omits the setting, cast/contact shadows, skin shading and internal decorative lines; all subject geometry is black on transparent. Normal rendering retains its existing transforms and materials. Review PNGs composite the complete subject onto white at its existing scene placement, leaving room for attachments.

Body-only exports are now explicitly named `formloom-{subject}-body-silhouette.png` and have a presentation inset to prevent edge clipping. Their placed mask variant remains unchanged for peel-containment tests. Complete exports have distinct `complete-silhouette` filenames with seed/variation labels. New tests cover repeatability, black pixels, transparent image borders, body inclusion, and a geometric leaf-interior point outside the body for apple/pear across seeds and variation extremes. PR #18 merged after run 41598590278 passed with the explicit Skia sampling-options fix (`a5a3662`). [Post-merge run 41598698403](https://github.com/Vraxo/Formloom/actions/runs/41598698403) passed restore, warnings-as-errors build, tests and artifact uploads at `f53b8cd`. No local SDK was installed. The user subsequently supplied the new complete-subject PNGs for actual visual review on 2026-11-03; findings follow.


### Complete-subject review findings (2026-11-03)

- Inspected supplied apple and pear silhouettes at seed 42/variation 0 and seed 1337/variation 1, plus pear seed 42/variation 0.35. Stems and leaves are visibly included and connected to each body, with no apparent detached components. Apple indentation/lobes and pear neck/belly remain recognizable.
- Inspected both supplied orange complete silhouettes (seed 42/variation 0, seed 1337/variation 1). Their outlines are smooth, slightly irregular and fully inside the canvas. The calyx lies on the body and therefore disappears into its solid black fill; its lack of a separate visible mark is expected, not missing geometry.
- All supplied complete silhouettes have clear white margins, including above the leaves. Black fills contain no scene, shadows, highlights or internal leaf veins.
- Normal apple/orange renders were also supplied and look consistent with the earlier reviewed styling. No historical pixel-identity claim is made.
- Only the pear body-mask export was attached in this batch; revised apple/orange body-mask exports were not visually inspected. The complete orange export itself is no longer edge-clipped. This distinction avoids claiming review of absent files.

The complete-subject silhouette follow-up passes visual review. No further renderer edits were needed for this review. Documentation changes are uncommitted; no new subject family or feature milestone has begun.


## Remaining CPU-performance evidence (timing instrumentation pending CI)

The existing ten 1024×1024 sample-export cases now write per-sample `formloom-*.timing.json` sidecars, uploaded alongside PNGs in `procedural-preview`. Stopwatch covers only the real CPU renderer and PNG encoding, not scene construction, artifact writes, desktop dispatch or GUI presentation. Reports include subject/prompt, seed/variation, dimensions, elapsed milliseconds, PNG byte length and runtime/OS/logical-processor context. No additional renders, packages, jobs or SDK installation are introduced.

These are advisory shared-runner observations, not isolated microbenchmarks: first-use/JIT, concurrent tests and runner load can affect individual samples. There is deliberately no arbitrary wall-clock assertion or promise of Windows desktop latency. Actual timing values have not been collected for these edits; the performance checkbox remains open until the fresh CI sidecars are inspected. Earlier aggregate test-suite durations alone are not treated as per-render performance measurements.
