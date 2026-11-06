# Phase 7 handoff — a tulip in a ceramic vase

One red or yellow tulip growing out of one existing blue or cream ceramic vase. Static,
illustrated, seeded, CPU-rendered through the unchanged prompt → Core → scene → Skia →
PNG/save path.

- **Starting point:** `main` at `54b26c7` (merge of PR #36), clean working tree.
- **Previous milestone:** Phase 6, still coffee or tea in ceramic cups — see
  [`cup-liquids-handoff.md`](cup-liquids-handoff.md) and [`.agents/todo-6.md`](../.agents/todo-6.md).
- **Checklist:** [`.agents/todo-7.md`](../.agents/todo-7.md).
- **Status:** implementation, tests and documentation complete; Linux Release CI green on the
  PR; **the human visual-review gate is still open** — see [Visual review](#visual-review).

## What ships

Exactly four prompts, and nothing else:

```
a red tulip in a blue ceramic vase on a wooden table
a red tulip in a cream ceramic vase on a wooden table
a yellow tulip in a blue ceramic vase on a wooden table
a yellow tulip in a cream ceramic vase on a wooden table
```

Each maps to one subject token and one whitelisted save name:

| prompt | subject token | file name |
| --- | --- | --- |
| red tulip / blue vase | `red-tulip-in-blue-vase` | `formloom-red-tulip-in-blue-vase-seed-42.png` |
| red tulip / cream vase | `red-tulip-in-cream-vase` | `formloom-red-tulip-in-cream-vase-seed-42.png` |
| yellow tulip / blue vase | `yellow-tulip-in-blue-vase` | `formloom-yellow-tulip-in-blue-vase-seed-42.png` |
| yellow tulip / cream vase | `yellow-tulip-in-cream-vase` | `formloom-yellow-tulip-in-cream-vase-seed-42.png` |

Subject tokens only reach a file name through `PngSaveService.KnownSubjects`, as before; a
raw prompt still degrades to `formloom-fruit-seed-<seed>.png`.

## Grammar

Exact, additive and whole-or-nothing:
`<a red|a yellow> tulip` + ` in ` + `<a blue|a cream> ceramic vase` + ` on a wooden table`.
Case, repeated whitespace and terminal punctuation are normalized by the existing
`PromptParser.Normalize`, which is unchanged.

Rejected whole (never simplified into an empty vase): other species, other colours
(pink, white, purple, orange, unspecified), bowls and cups as the container, `inside` /
`within` / `with` / `holding` / `of`, the reversed `a blue ceramic vase with a red tulip`,
quantities and plurals (two tulips, a bunch, a bouquet), two vases, extra adjectives
(`large`, `tall`), any second object in the scene, and a missing table phrase. The tests
carry thirty-one such prompts.

`SceneGenerator.Generate` dispatches `ParseVaseFlower` after containment and cup contents
and before `ParseObjects`, so a flower prompt can never be read as an ordinary mixed group.

## Model

```csharp
public sealed record VaseFlower(CeramicInstance Vase, FlowerKind Flower, TulipColor Color, TulipPlant Plant);
public sealed record TulipPlant(Vector2 StemBase, Vector2 StemTop, float StemThickness,
    ImmutableArray<CubicSegment> Stem, ImmutableArray<TulipLeaf> Leaves, TulipBloom Bloom);
public sealed record TulipBloom(Vector2 Base, float Width, float Height, ImmutableArray<TulipPetal> Petals);
public sealed record TulipLeaf(Vector2 Attachment, Vector2 Tip, float Width,
    ImmutableArray<CubicSegment> Outline, ImmutableArray<CubicSegment> Vein);
public sealed record TulipPetal(ImmutableArray<CubicSegment> Outline, float Tone);
```

- `StillLifeScene.VaseFlower` is an optional property beside `Containment` and
  `CupContents`. The flower is **not** a `SceneEntry`: it has no transform, depth, bounds or
  shadow of its own, so the one-to-three independent-object rules are untouched.
- The arrangement refers to the vase instance the scene already holds — the same reference,
  asserted with `Assert.Same` — so no transform, seed, glaze, profile or depth is duplicated.
  `VaseFlower.Create` only substitutes the kind when a caller hands it a non-vase.
- `FlowerKind` is closed to `Tulip`; `TulipColor` is closed to `Red` and `Yellow`. Both are
  values: no Skia paint, path, shader or colour appears in Core.
- The three contents relationships are mutually exclusive. The existing bowl-versus-cup
  message is untouched; a flower conflicting with either returns
  `"A scene cannot hold a flower and another contents relationship at the same time."`.
- Validation is explicit and never silently discards the flower: non-vase container,
  undefined flower kind, undefined colour, a scene that is not exactly that one vase, a
  missing stem, a leaf count other than two, a petal count outside three to five,
  non-finite or non-positive geometry, a stem base outside the opening, a stem that never
  clears the rim, a bloom or leaf below the rim, and bounds outside the canvas margins each
  return their own message.

## Geometry

Everything is in the same normalized, y-down scene space as fruit and ceramics, and every
length comes from the vase: no canvas pixel offset is hard-coded anywhere.

| quantity | derivation | value at the default vase |
| --- | --- | --- |
| opening centre | `MouthCenter × body.Size + body origin` | `(0.500, 0.226)` |
| opening radius | `MouthRadius × body.Size` | `(0.0476, 0.0280)` |
| rim crest | `MouthCenter.Y − MouthRadius.Y − RimThickness × Size.Y` | `≈ 0.189` |
| headroom | rim crest − `TopMargin` (0.036) | `≈ 0.153` |
| plant height | 86 % of the headroom, ±3 % seeded | `≈ 0.131` |
| bloom | 66 % of the plant height, width = 0.92 × height | `≈ 0.087 × 0.080` |
| visible stem | the remaining 34 % | `≈ 0.044` |
| stem thickness | `MouthRadius.X × 0.125 × Size.X` | `≈ 0.0060` |

- **Stem.** One cubic from a base inside the opening (`MouthCenter`, sunk by 35 % of the
  opening radius) to a point above the rim, with seeded lateral sway at both control points
  for a gentle, natural bend — never a rigid vertical line.
- **Leaves.** Exactly two, at 22 % and 62 % of the visible stem, snapped onto the stem curve
  by height so the attachment is exact (`TulipGeometry.DistanceToStem` proves it in tests).
  They splay in opposite directions (≈ −1.18 rad and +1.10 rad from vertical, seeded ±0.16)
  with slightly different lengths (44 % and 40 % of the plant height) and a seeded bow, so
  neither is hidden behind the bloom. The blade is an authored lance profile — widest a
  little above the middle, tapering to a point — with one restrained centre vein; no
  serrations, veins-per-leaf or texture noise.
- **Bloom.** Five overlapping wrapped petals authored as two wide back petals, two side
  petals and one narrow front petal, smoothed with the same `CurveBuilder.ClosedCubic`
  used by the fruit and vessel outlines. The tone hierarchy (0.00, 0.10, 0.52, 0.60, 1.00)
  is what makes the overlap read as a cup rather than a flat disc with radial lines. The
  bloom sits on the stem's end, with a seeded tilt (±0.09 rad at full variation) and
  per-petal scale/spin jitter, so the crown is organic without changing identity.
- **Variation.** Three named streams — `tulip.stem`, `tulip.leaves`, `tulip.bloom` — are
  derived from the scene seed. They are independent of the vase's own streams and of every
  fruit, cup and bowl stream, and independent of the glaze: blue and cream vases produce
  identical plants for the same seed, and red and yellow tulips share identical geometry
  (tested by comparing a rounded signature of every control point).
- **Bounds.** `TulipGeometry.Measure` unions the stem, leaf and petal extents and pads them
  by half the stem stroke. `SceneValidator` requires the result to stay above
  `TopMargin − 0.012`, inside 0.02–0.98 horizontally, and no lower than the stem base — so
  the plant cannot reach a canvas edge at any seed, any variation, or any output size.

## Materials and draw order

`TulipMaterial` (Skia only) carries three petal colours per tulip colour plus one shared
green set for stem and leaf. Petal colour is the only thing red and yellow do not share, so
a colour change can never move a pixel outside the bloom — and because the green set is
shared, a petal-colour change leaves the stem and leaves byte-identical.

Draw order inside `DrawVaseFlower`, then the vase, unchanged:

1. **Clip.** A half-plane whose top edge is the mouth's own top tangent, derived from the
   vase profile — profile-aware, not a screen-space rectangle around the bloom.
2. **Stem**, stroked with a horizontal light-to-dark gradient and round caps.
3. **Leaves**, back to front: linear gradient from the attachment (light) to the tip
   (dark), then the centre vein as a thin stroke.
4. **Petals**, back to front: a radial gradient offset along the scene light direction, with
   the tone lifting the base colour, plus a very light edge stroke for separation.
5. **The vase**, drawn exactly as an empty vase is drawn — so the stem disappears behind the
   rim and every vase pixel below the rim band is byte-identical to the empty-vase render.

Consequences, all asserted:

- No flower pixel can reach the vase's exterior wall, the table or the background: the clip
  forbids it and the vase paints over anything inside its own silhouette.
- The bloom and both leaves stay above the rim; no petal-coloured pixel exists below the
  mouth's top edge.
- The only intentional overlap is the rim band the stem passes behind (about 14 px at
  1024 × 1024: the lip and rim strokes are drawn after the flower, so a fraction of the stem
  shows through their translucency exactly where it should).
- **Shadow mask:** the flower contributes nothing, so a planted vase's shadow mask is
  byte-identical to the empty vase's. The flower has no tabletop shadow of its own.
- **Silhouette:** the flower *is* included, because the bloom is a visible part of the
  subject. The silhouette of a planted vase is the empty vase's silhouette plus the flower
  above the rim; no vase silhouette pixel is lost (`planted ≥ empty` alpha everywhere).
- **Detail omission** (`RenderWithoutInstanceDetail`) drops the leaf veins and petal edge
  strokes; nothing outside the flower's own bounds changes.
- Diagnostic passes do not contaminate a later normal render.

## Desktop surface

`PngSaveService.KnownSubjects` gains the four tokens; `App.razor`'s prompt help gains
`a red tulip in a blue ceramic vase on a wooden table`. No control, panel, setting or
workflow step was added, and no existing token, filename or behaviour changed.

## Tests

`tests/Formloom.Tests/VaseFlowerTests.cs` (Core) — four-combination parse and generate,
31-prompt rejection matrix, normalization variants, typed-content invariants (one entry,
same vase reference, flower not an entry), stem attachment and opening invariants across
seeds and variations, exactly two leaves and one bloom for six seeds × three variations,
red/yellow geometry equality, determinism and bounds across seeds and variation extremes,
glaze and stream independence, the validation-rejection matrix, mutual exclusion, and
ordinary-prompt preservation.

`tests/Formloom.Tests/VaseFlowerRenderingTests.cs` (pixels) — per combination: PNG bytes
determinism, subject token, changed-pixel floor against the empty vase, *nothing* changed
below the rim band, nothing changed outside the flower's bounds above it, bloom/leaf/stem
presence counts, petal-colour identity, and no petal pixel below the mouth top; five output
sizes from 64 × 64 to 4096 × 1024; red-versus-yellow differences confined to the bloom;
glaze-independence of every pixel above the rim; unchanged shadow mask and additive-only
silhouette; detail omission confined to the flower; diagnostic-pass stability; alternate
seeds reshaping the bloom without changing subject, species or leaf count; empty vases
gaining no petal pixels.

`tests/Formloom.Desktop.Tests/VaseFlowerPipelineTests.cs` — the four prompts through the
real session/generation service, preview retention across six rejected phrasings, empty
versus planted metadata separation, and the save-filename whitelist including a hostile
token. `AppComponentTests` gained both a red and a yellow row and asserts the prompt help
mentions the tulip.

## Artifacts

`procedural-preview` (run [42182285484](https://github.com/Vraxo/Formloom/actions/runs/42182285484),
16.3 MB) gains, beside the per-case files:

- `formloom-vaseflower-<name>-seed-<seed>-variation-<v:0.00>.png` — eight cases: the four
  defaults, an alternate seed per tulip colour (1337, 20261105) and the two variation
  extremes (0 and 1);
- `…-silhouette.png` for each case, asserted different from the same empty vase's;
- `….timing.json` — prompt, subject, flower kind, tulip colour, vase kind and glaze, opening
  centre/radius, mouth top, stem base/top/thickness, bloom base/width/height/top, leaf and
  petal counts, measured bounds, dimensions, framework, seed, variation,
  `RenderAndEncodeMilliseconds`, the equivalent **empty-vase** timing and the difference;
- `formloom-vaseflower-sheet.png` — a 4 × 2 row-major grid of 320 px cells, and
  `formloom-vaseflower-sheet.timing.json`, the manifest that identifies every cell (name,
  prompt, seed, variation, subject, flower, tulip colour, glaze, filename, cell/column/row,
  dimensions, timing). The CI image has no font stack, so the sheet carries no text and the
  manifest is the authoritative key.

Cell identity, row-major: **0** red / blue 42 · 0.35 · **1** red / cream 42 · 0.35 ·
**2** yellow / blue 42 · 0.35 · **3** yellow / cream 42 · 0.35 · **4** red / blue seed 1337 ·
**5** yellow / cream seed 20261105 · **6** red / cream at variation 0 · **7** yellow / blue
at variation 1.

## Timing observations

The exporter records a filled-and-empty pair per case exactly as Phase 6 does, but those
1024 × 1024 figures have **not been read back**: this environment can reach check-run
annotations only, not artifact blobs, so no full-size timing figure is quoted here and the
measurement item in the checklist stays open. The sheet's 320 px cells are also unread.
What is known: the phase adds one clipped stem stroke, two leaf fills and five petal fills
per render, with no extra surface, blur or full-canvas pass.

## Visual review

**Pending.** No pixel of the planted flower has been inspected by eye in this environment;
producing the artifact is not a review. The sheet and the individual 1024 × 1024 PNGs are in
`procedural-preview`; the gate is the review items in `.agents/todo-7.md` §7, namely:
the bloom reads as a tulip (cupped, petal-shaped, not a daisy, star, blob, rose or fruit);
the stem enters the vase naturally and is hidden in the right places; exactly two leaves are
visible, attached and balanced; both tulip colours are legible on both glazes; alternate
seeds and the variation extremes show no broken petals, malformed leaves, excessive bend,
clipping or edge collisions; the vase's rim, mouth, body, glaze, foot and grounding show no
regression; and the flower's materials are restrained and coherent with the still-life
lighting rather than neon, plastic or cartoon-like.

To close it: review `formloom-vaseflower-sheet.png` (8 cells, 1280 × 640, row-major; use
`formloom-vaseflower-sheet.timing.json` to map a cell) plus, where a cell looks uncertain,
the matching 1024 × 1024 PNG.

## Known limitations

- One species, one bloom, one stem, two leaves, one colour pair, one fill level, one vase
  per scene, and no arrangement control beyond seed and variation.
- The flower is a flat illustrated surface set: no volume, no translucency, no petal
  physics, no wind, no growth, no drooping, no pollen or stamens, and no shadow of its own.
- Shadow-mask output deliberately omits the flower; silhouette output deliberately includes
  it. Both are documented conventions, not defects.
- The stem is clipped at the mouth's top tangent, so it is not drawn *inside* the opening.
  It ends behind the rim, where the lip strokes cover it — the cheapest way to guarantee no
  leak onto the ceramic while keeping every vase pixel below the rim band byte-identical.
- Flower height is capped by the vase's headroom, so the plant is proportioned to the vessel
  rather than to the canvas; a taller flower would need a shorter vase or a smaller margin.
- The bloom's five petals, the two leaf angles and the headroom shares are authored
  constants. They are tuned blind (see above) and may need one cosmetic pass after review.

## Authored but not executed

- **The 1024 × 1024 renders and the sheet were not inspected.** They exist in the artifact;
  the visual-review items stay unchecked until a human looks at them.
- **The timing sidecars were not read back**, so the 1024 × 1024 filled-versus-empty-vase
  comparison is recorded in the artifact but not in this document.
- **Native desktop behaviour** — the PhotinoX save dialog, window lifecycle, resize and
  accessibility — is not exercised by headless tests or Linux CI, exactly as in earlier
  phases.

## Verification

- PR: [#37](https://github.com/Vraxo/Formloom/pull/37).
- First CI run on `9f81262`: [42181288364](https://github.com/Vraxo/Formloom/actions/runs/42181288364)
  — failed the warnings-as-errors build on three analyzer findings (SA1203 for a constant
  field's position and SA1407 for undeclared arithmetic precedence); no test was reached.
- Second run on `f10fced`: [42181427645](https://github.com/Vraxo/Formloom/actions/runs/42181427645)
  — success: restore, Release build with warnings as errors, both test projects,
  `test-results-Release` and `procedural-preview`.
- Third run on `af206d0`: [42182087830](https://github.com/Vraxo/Formloom/actions/runs/42182087830)
  — the test step failed on one of this milestone's own pixel scans, which counted the warm
  tabletop as a yellow petal; the scan now stops at the table horizon.
- Fourth run on `6281b7a`: [42182285484](https://github.com/Vraxo/Formloom/actions/runs/42182285484)
  — success, all steps green, `procedural-preview` 16.3 MB (up 2.1 MB from Phase 6, i.e. the
  eight new renders, their silhouettes and the sheet) and `test-results-Release` 108 KB.
- Merge commit and post-merge run: recorded once the milestone is merged.
