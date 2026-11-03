# Organic forms milestone — implementation and verification notes

## Current status

Implementation and automated coverage are authored; **.NET build/test execution, actual generated samples and visual acceptance are pending**. The sandbox has no .NET SDK. Per established project direction, SDK validation is delegated to Linux Release CI, not a local SDK installation. Do not interpret authored tests or the sample-export code as passing tests/generated images.

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

## Authored tests and review artifacts

- Parsing to explicit subject/color and unsupported combinations.
- Sampled cubic continuity/closure, finiteness/bounds, deterministic segments, meaningful pear neck/belly ratio, profile differences and stream isolation.
- Valid PNGs across 256/1024/1280 and rectangular sizes, variation extremes, seed changes and pixel repeatability.
- Black silhouette distinction and exports for human recognition review.
- Orange detail-on/off comparisons prove outside-contour background/table pixels are unchanged.
- Real desktop pipeline/save metadata tests for new prompts and failure retention, plus actual component input/submission coverage. Existing opacity, grounding, grain and session/save tests remain in place and must pass.

CI's existing **procedural-preview** artifact now collects `formloom-*.png` from the original renderer test output directory, including apple, green/yellow pear and orange samples, alternate seeds for the new fruits, low/high variation and silhouettes. Files are generated by test execution, not embedded artwork, and remain under ignored `artifacts/`. They have not yet been generated in this environment.

Restore/build/test commands and CI policy are unchanged. No permanent Debug job or additional runner is introduced. Before declaring this milestone complete, inspect actual samples at normal size, verify pear silhouette/orange peel and apple preservation, and record a passing latest Release CI run. Native UI tests are not claimed by headless tests. Stop at this milestone; no additional fruit family or feature is authorized.
