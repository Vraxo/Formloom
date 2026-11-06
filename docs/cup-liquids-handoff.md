# Phase 6 handoff — still coffee or tea in ceramic cups

One existing cup, one still beverage. Four combinations: blue and cream glazes, each with coffee or tea. This is a static illustrated surface, not a fluid simulation, and it reuses the existing prompt → Core → deterministic generation → Skia CPU renderer → PNG/save pipeline unchanged.

- **Starting commit:** `995d341` (merge of `.agents/todo-6.md` into `main`).
- **Ending commit:** `_recorded below_`.
- **Spec:** [`.agents/todo-6.md`](../.agents/todo-6.md).
- **Previous milestone:** [Phase 5 handoff](containment-handoff.md).

## Grammar

```
<blue|cream> ceramic cup  filled with  <coffee|tea>  on a wooden table
```

Four prompts, all supported:

- `a blue ceramic cup filled with coffee on a wooden table`
- `a cream ceramic cup filled with coffee on a wooden table`
- `a blue ceramic cup filled with tea on a wooden table`
- `a cream ceramic cup filled with tea on a wooden table`

Case, repeated whitespace and terminal punctuation are normalized exactly as for every other prompt. `filled with` is the only relationship phrase; `with`, `containing`, `holding` and `of` are rejected, as is the reversed `coffee in a blue ceramic cup`. Everything else in the prompt must be the existing cup descriptor and the existing setting phrase, so extra adjectives, other colours, plurals, quantities, two cups, two drinks, bowls or vases as the container, and a filled cup combined with any other object are all refused **as a whole**. A malformed beverage prompt never degrades into an acceptable empty cup: `SceneGenerator` fails and the desktop keeps the previous preview.

`PromptParser` gained one additive entry point, `ParseCupContents`, returning a typed `CupContentsPrompt`. `Parse`, `ParseFruit`, `ParseScene`, `ParseObjects` and Phase 5 `ParseContainment` are untouched; a filled-cup prompt returns `null` from all of them, because it is one object with contents rather than a list of neighbours.

## Typed model

```csharp
public enum BeverageKind { Coffee, Tea }

public sealed record CupContents(CeramicInstance Cup, BeverageKind Beverage);

public sealed record CupContentsPrompt(CeramicPrompt Cup, BeverageKind Beverage);
```

`StillLifeScene.CupContents` is a single optional property. It **refers to the cup instance the scene already holds** — `GenerateCupContents` builds the ordinary single-object scene for that cup and then attaches the contents, so the cup keeps its kind, glaze, body, seed, depth and handle, and the filled scene differs from the empty one only inside the mouth. The drink has no transform, no depth and no entry: there is no second scene object, no scene graph, no registry, no `object` field and no type-name strings.

Validation (`SceneValidator.ValidateCupContents`) requires:

- the container is exactly `CeramicKind.Cup`, and the beverage is a defined `BeverageKind`;
- the scene holds exactly one entry and it is *that* cup instance (a missing cup, a duplicate cup, or an extra independent object is rejected, never repaired);
- the surface has finite, positive bounds, the clip is inside the inner opening, the surface top stays below the far lip and the surface bottom stays inside the mouth;
- `CupContents` and `BowlContainment` are mutually exclusive — a scene cannot be both a filled cup and a fruit in a bowl.

## Surface geometry

`CupLiquidSurface` derives everything from `CeramicProfile` for the cup, so no canvas pixel offset is hard-coded. For the authored cup profile (`MouthCenter` 0.5/0.10, `MouthRadius` 0.30/0.055, `RimThickness` 0.010, `CavityDepth` 0.05):

| Element | Value | Why |
| --- | --- | --- |
| Surface center | `(0.5, 0.1060)` | sunk `0.6 × rim` below the mouth center, so the cup reads filled, not overflowing |
| Surface radius | `(0.3300, 0.0435)` | flattened like the opening; wider than the opening on purpose |
| Clip (inner opening) | center `(0.5, 0.10)`, radius `(0.2890, 0.0440)` | the mouth ellipse inset by `1.1 × rim`, so no lip pixel can be covered |
| Visible top | `0.0625` | leaves a crescent of far inner wall `0.0065` (≈ 6–7 px at 1024) above the drink |
| Visible bottom | `0.1440` | the drink tucks behind the near wall: the surface's own bottom is `0.1495`, beyond the clip, so the front lip wins |

The surface ellipse being wider than the clip is deliberate: the left and right extremes are governed by the inner opening, so the drink meets the inner wall instead of floating inside a ring of ceramic. The geometry is identical for blue and cream (both use the same profile), at every supported seed and at both variation extremes — one fixed, believable fill level, with no prompt or UI control. There is no seeded irregularity: a static deterministic surface is what the spec prefers, and it keeps the bounds provable.

## Rendering and draw order

The drink is inserted into the existing cup mouth stage in `StillLifeRenderer`:

1. cavity radial gradient;
2. lip sheen;
3. darker inner cavity oval;
4. **drink** (`DrawCupLiquid`) — body, meniscus, then two accents when detail is enabled;
5. the existing rim stroke and the front-half lip arc.

Because the drink sits under step 5, the ceramic edge always reads as in front of the liquid. `DrawCupLiquid` clips to the inner opening, draws an opaque body gradient, strokes a thin meniscus at its own boundary (clipped, so only the visible edge is affected), and adds two broad reflections — one soft window sheen and one smaller glint — positioned toward the existing light direction. Everything is a gradient; there are no hard shapes, no foam, no bubbles, no steam, no motion cues.

The cup's finish sheen is **held off the drink** by clipping that pass to the complement of the inner opening, so a glaze can never tint the beverage: interior liquid pixels are byte-identical between the blue and cream variants, which the tests assert rather than assume.

Diagnostics needed no new code, because the drink is interior:

- **Silhouette** — the mouth is already an opaque interior of the cup silhouette, so the filled cup's alpha mask is byte-identical to the empty cup's, and the handle hole stays transparent (`CreateRing`'s winding is untouched).
- **Shadow mask** — the cup's shadow path returns before the body is drawn, so a filled cup casts exactly the empty cup's tabletop shadow and the drink adds none.
- **Detail omission** — `RenderWithoutInstanceDetail(scene, cupDepth)` drops the accents but keeps body and meniscus, so the drink stays identifiable and nothing changes outside the mouth.

Empty cups take the unchanged path: `DrawMouth` only calls `DrawCupLiquid` when `scene.CupContents` refers to the ceramic being drawn, so empty cups, bowls and vases are untouched byte for byte.

## Subject tokens and save names

| Prompt | Token | Filename |
| --- | --- | --- |
| blue + coffee | `coffee-in-blue-cup` | `formloom-coffee-in-blue-cup-seed-42.png` |
| cream + coffee | `coffee-in-cream-cup` | `formloom-coffee-in-cream-cup-seed-42.png` |
| blue + tea | `tea-in-blue-cup` | `formloom-tea-in-blue-cup-seed-42.png` |
| cream + tea | `tea-in-cream-cup` | `formloom-tea-in-cream-cup-seed-42.png` |

All four are whitelisted in `PngSaveService.KnownSubjects`; anything else still falls back to `fruit`, so no filename is ever derived from raw prompt text. Every existing fruit, group, ceramic, still-life and fruit-in-bowl token is unchanged.

## Tests

`tests/Formloom.Tests/CupLiquidTests.cs` (Core, parser, validation):

- all four combinations parse and generate, with the cup left identical (body and seed) to the same empty-cup prompt;
- case/whitespace/punctuation normalization;
- a filled-cup prompt is not an object list: the five older entry points return `null`;
- a 24-row rejection matrix, each row also refused by `Generate`;
- ordinary cup prompts (single and mixed) still parse with `CupContents == null`;
- `CupContents` and `BowlContainment` are mutually exclusive;
- validation rejects a bowl or vase container, an undefined beverage value, a duplicate cup, an extra object and a missing cup, each with its exact message;
- surface geometry is derived from the opening: center on the mouth axis, flattened, sunk below the lip, clear of the far and near lip, clip inside the rim, wider than the clip;
- the surface is independent of glaze, seed and variation, and deterministic across runs;
- the visible extent stays inside the opening ellipse with a positive rim margin and a visible far-wall crescent.

`tests/Formloom.Tests/CupLiquidRenderingTests.cs` (pixels and artifacts):

- deterministic 1024 × 1024 PNGs for all four combinations, with the expected subject token;
- 256 × 256, 640 × 480 and 2048 × 1024 still carry liquid pixels;
- **non-vacuous in-mouth invariant**: against the same empty cup, more than 700 pixels change, every changed pixel is inside the mouth ellipse, none is outside the clip (so the rim and lip keep their own pixels), and none is below the table horizon (so the drink casts no tabletop shadow);
- coffee and tea are visibly distinct (mean luminance/colour delta in the liquid region) and coffee stays the darker drink;
- the glaze does not recolour the beverage: zero differing pixels in the liquid interior between blue and cream, while the ceramics differ by hundreds of pixels;
- the filled cup's silhouette and shadow mask are byte-identical to the empty cup's, and the handle hole stays transparent;
- detail omission keeps the drink, removes the accents, and moves nothing outside the mouth;
- diagnostic passes leave later renders byte-identical;
- beverage identity survives seeds 42, 1337 and 20261105.

`tests/Formloom.Desktop.Tests/CupLiquidPipelineTests.cs`: all four prompts through the real session/generation service, preview retention and metadata stability across five rejected phrasings, empty-versus-filled metadata separation, and the save-filename whitelist including a hostile token. `AppComponentTests` gained both a coffee and a tea row and now asserts the prompt help mentions the filled cup.

## Artifacts

The existing `procedural-preview` upload gains, beside the per-case files:

- `formloom-cupliquid-<name>-seed-<seed>-variation-<v:0.00>.png` — eight cases: the four defaults, an alternate seed per beverage (1337, 20261105) and the two variation extremes (0 and 1);
- `…-silhouette.png` for each case, asserted equal to the same empty cup's silhouette;
- `….timing.json` — prompt, subject, cup contents and surface geometry, dimensions, framework, seed, variation, and `RenderAndEncodeMilliseconds` **plus the equivalent empty-cup timing** and the difference;
- `formloom-cupliquid-sheet.png` — a 4 × 2 row-major grid of 320 px cells, and `formloom-cupliquid-sheet.timing.json`, the manifest that identifies every cell (name, prompt, beverage, glaze, seed, variation, filename, cell/column/row, dimensions, timing). The CI image has no font stack, so the sheet carries no text and the manifest is the authoritative key.

## Timing observations

Per-case timings are written to the sidecars, but GitHub artifact storage is not reachable from the agent sandbox, so **no filled-cup timing figure is quoted here**. Each timing sidecar records the filled and empty numbers side by side for the same runner and runtime; read `RenderAndEncodeMilliseconds`, `EmptyCupRenderAndEncodeMilliseconds` and `AdditionalMillisecondsOverEmptyCup` there. Phase 4's baseline was 119–143 ms per 1024 × 1024 render plus encode for vessels and groups; a filled cup draws one cup plus one clipped gradient surface, so it is expected to sit within a few milliseconds of the empty cup.

## Visual review status

**Pending.** The sheet and the eight full-size renders exist in the artifact, but the agent could not download them, and this sandbox has no .NET runtime to render locally, so no claim is made about the pixels. What still needs human eyes: that coffee reads as coffee and tea as tea without a label; that the surface looks recessed liquid rather than a sticker, lid or painted patch; that the elliptical perspective agrees with the mouth and leaves a consistent ceramic margin; that the front lip stays crisp in front of the drink; that neither drink looks neon, metallic or oversaturated; and that empty cups, bowls, vases, fruit and fruit-in-bowl scenes are unchanged.

## Known limitations

- One cup, one beverage. No second cup, no second drink, no fruit in a cup, no drink in a bowl or vase.
- Only `coffee` and `tea`; no milk, foam, crema or latte art, no ice, lemon, tea bags, spoons, saucers or straws.
- One fixed fill level, no user control, no waves or ripples that imply motion, no steam, no pouring.
- The drink is a surface: there is no volume, refraction, transmission or fluid behaviour, and no cast shadow of its own.
- The reflection set is two fixed gradients positioned by the scene light direction; it does not respond to a changing environment.
- The drink's silhouette contribution is intentionally zero, so a filled cup and the same empty cup are indistinguishable in silhouette and shadow-mask diagnostics. That is the documented convention, not a defect.

## Verification

- PR: _recorded below_
- Merge commit: _recorded below_
- CI run: _recorded below_
- Post-merge run: _recorded below_

The workflow is unchanged: one Ubuntu **Linux / Release** job — restore, warnings-as-errors build, both test projects, test-results upload and the `procedural-preview` upload. No new package, project, runner or workflow step was added.
