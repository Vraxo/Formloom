# Phase 8 handoff — a white daisy in a ceramic vase

One white daisy with a golden-yellow centre growing out of one existing blue or cream ceramic
vase. Static, illustrated, seeded, CPU-rendered through the unchanged prompt → Core → scene →
Skia → PNG/save path.

- **Starting point:** `main` at `dcb207f` (merge of PR #44), clean working tree; the Phase 8
  spec arrived from `origin/main` as commit `11d631d`.
- **Previous milestone:** Phase 7, a tulip in a ceramic vase — see
  [`vase-flower-handoff.md`](vase-flower-handoff.md) and [`.agents/todo-7.md`](../.agents/todo-7.md).
- **Checklist:** [`.agents/todo-8.md`](../.agents/todo-8.md).
- **Status:** implementation, tests, artifacts and documentation complete; **visual review passed**
  on the fixed build. Two defects were found and fixed on the way: a leaf hidden behind the head,
  and a size-dependent loss of the stem inside the mouth that only appeared in the sheet's 320 px
  cells. The canonical blue case was reviewed at full resolution, then both group sheets were
  reviewed cell by cell on the merged build, closing the cream glaze, alternate seeds, variation
  extremes and 320 px legibility items. See [Visual review](#visual-review). The timing numbers are
  left in the artifact with the reason stated under
  [Timing observations](#timing-observations).

## What ships

Exactly two prompts, and nothing else:

```
a white daisy in a blue ceramic vase on a wooden table
a white daisy in a cream ceramic vase on a wooden table
```

Grammar is exact: `a white daisy` + `in` + `<a blue|a cream> ceramic vase` + `on a wooden table`.
Case, repeated whitespace and terminal punctuation are normalized exactly as before. Everything
else is refused whole, and the refusal never produces an empty vase or a tulip instead:

- other daisy colours — red, yellow, pink, blue — and an unspecified `a daisy`;
- other species: rose, sunflower, lily, orchid, `a white flower`;
- a bowl or a cup as the container, and any vase without a glaze;
- `inside`, `within`, `with`, `holding`, and the reversed `a blue ceramic vase with a white daisy`;
- plurals and quantities: `white daisies`, a bunch, a bouquet, two daisies;
- two vases, extra adjectives (large, small), a second object, a mixed daisy-plus-tulip prompt,
  a malformed separator, and a missing table phrase.

Subject tokens and save names: `white-daisy-in-blue-vase` and `white-daisy-in-cream-vase`, i.e.
`formloom-white-daisy-in-blue-vase-seed-42.png` and
`formloom-white-daisy-in-cream-vase-seed-42.png`. Both are whitelisted in
`PngSaveService.KnownSubjects`; any other token still falls back to `formloom-fruit-…`, and every
earlier token and filename is unchanged.

## The design decision: a sibling, not a widened tulip

`VaseFlower` is untouched. The daisy is its own typed vase content:

| Concern | Tulip | Daisy |
| --- | --- | --- |
| content | `VaseFlower(CeramicInstance Vase, FlowerKind Flower, TulipColor Color, TulipPlant Plant)` | `VaseDaisy(CeramicInstance Vase, DaisyAppearance Appearance, DaisyPlant Plant)` |
| kind | `FlowerKind.Tulip` | `FlowerKind.Daisy` (stored, and the validator refuses anything else) |
| appearance | `TulipColor {Red, Yellow}` | `DaisyAppearance {WhiteWithGoldenDisk}` |
| geometry | `TulipPlant`, `TulipBloom`, `TulipPetal`, `TulipLeaf` | `DaisyPlant`, `DaisyBloom`, `DaisyPetal`, `DaisyDisk`, `DaisyLeaf` |
| streams | `tulip.stem`, `tulip.leaves`, `tulip.bloom` | `daisy.stem`, `daisy.leaves`, `daisy.petals`, `daisy.disk` |
| material | `TulipMaterial` | `DaisyMaterial` |

Why not one widened type: the two species share no petal shape, no petal count, no material and
no colour vocabulary. A `VaseFlower` with an optional daisy payload would have made every tulip
path nullable and every validation branch conditional, and it would have let a `TulipColor` reach
a daisy. Keeping the appearance closed per species makes that impossible by construction rather
than by a check.

What the species **do** share, deliberately, is only what is shape-agnostic:

- `VaseMouth` — one derivation of `Profile`, `OpeningCenter`, `OpeningRadius` and the mouth top,
  now used by `VaseFlower` and `VaseDaisy` alike. The tulip's numbers are byte-identical to before
  the refactor.
- `VaseStem` plus `StrokeStem` and `DrawStemInsideOpening` in the renderer — one mouth-continuation
  pass for both, so a daisy stem enters a vase exactly the way a tulip stem does.

Both contents keep the rules that were already there: the arrangement refers to the vase instance
the scene already holds (never a copy), it is not a `SceneEntry` and has no transform, depth or
shadow of its own, and `VaseFlower`, `VaseDaisy`, `BowlContainment` and `CupContents` are mutually
exclusive.

## Geometry

Authored constants in `DaisyGeometry`, all normalized and vase-profile aware:

| quantity | value | why |
| --- | --- | --- |
| petal count | **12**, fixed | enough to read as a daisy at still-life size; jitter reshapes a petal but never re-counts the flower |
| head radius | `min(mouthRadius.X × 1.55, headroom × 0.34)` | a flat head is wider than a tulip bloom but much shorter, so it must not eat the whole headroom |
| vertical squash | 0.86 | the head is a disc seen slightly from the front, not a circle |
| disk radius | `0.34 × headRadius` (× 0.86 vertically) | a golden centre that covers the petal roots without becoming a button |
| petal roots | `0.55 × diskRadius` from the centre | the petals visibly emerge from behind the disk |
| stem thickness | `mouthRadius.X × 0.085` | slender against the vessel |
| leaves | exactly 2, in the clear stem between the head's bottom edge and the rim | attached at different heights, splayed almost horizontally, long enough to reach past the head's silhouette |
| flatten share | petals ordered back to front by `sin(angle)` | the upper half sits behind the lower half |
| petal tone | 0.62 far / 0.90 near | a value hierarchy instead of flat white |

The stem base is the mouth's centre offset into the opening; the stem height is whatever leaves
the head under `TopMargin`. Nothing is in canvas pixels, so a different vase body scales the
plant with it. `DaisyPlant.Bounds` is measured from the drawn contours and the disk, so no caller
can hand in a rectangle that disagrees with what is painted.

## Materials and occlusion

`DaisyMaterial` holds every daisy colour; Core holds none. Petals shade from a warm root to a cool
tip with a cool edge stroke, so white never merges into a glaze highlight; the disk is a radial
golden dome with a soft inner ring; stem and leaves share one green material with directional
shading. Changing the vase glaze cannot recolour the flower, and a tulip colour cannot reach one.

Draw order is the tulip's, unchanged: the flower goes down **before** the vase, clipped to the
half-plane above the mouth's top tangent (+0.002 so the rim never slices the head), then the vase
paints its cavity, far rim, near lip, contour, body and foot over it, and the mouth's continuation
pass paints the stem inside the opening last, through one mask (the opening inset by the near lip,
unioned with a narrow column across the rim band). The daisy adds no tabletop cast shadow and does
not touch the shadow mask. Silhouettes include the head; detail omission drops the petal edge
strokes and the leaf veins.

## A bug this milestone found and fixed

Proving that the daisy's ink stays inside its own measured bounds at every supported size
exposed a defect in the shared mouth-continuation pass, not in the daisy: `DrawStemInsideOpening`
stepped out of the vase's space with `ResetMatrix()` followed by a **fixed** `Scale(1024, 1024)`,
while the renderer's base transform is `Scale(scene.Width / 1024, scene.Height / 1024)`. On the
1024 × 1024 reference canvas the two agree, which is why nothing caught it; on every other canvas
the continuation was painted at reference coordinates — a stray stub near `(508, 166)` in device
pixels, and an empty mouth where the stem should enter. The matrix now resets to the scene's own
size, and the per-size test asserts both halves of the property: no daisy ink outside its bounds,
and the stem visibly inside the opening at 64 × 64 through 4096 × 1024. Canonical 1024 renders are
byte-identical before and after; the tulip, which shares the pass, was affected at every other
size until this fix.

## Tests

`VaseDaisyTests` (Core) and `VaseDaisyRenderingTests` (pixels), plus
`VaseDaisyPipelineTests` (desktop) and two extra component cases:

- both prompts parse, generate, validate and carry the right token;
- 33 unsupported daisy phrasings refused whole, with no empty-vase or tulip fallback;
- typed-content invariant: one entry, and that entry is the arrangement's own vase instance;
- exactly one stem, exactly two leaves that hang from the sampled stem, one bloom of 12 petals;
- radial arrangement with even spacing, roots behind the disk, tips past it, both value bands;
- determinism for identical inputs, bounds and margins across six seeds and five variations;
- glaze-independent geometry, and daisy streams that leave the tulip's own geometry untouched;
- eleven specific validator rejections plus exclusion from the tulip, bowl and cup contents;
- pixel invariants against the same empty vase: head, both leaves and the stem above the rim are
  drawn; the only changed pixels below the rim are inside the opening; the stem covers the rim
  band and the opening row by row down to the near lip's inner edge and is gone a few rows into it;
- white petals and the golden disk are actually painted, are distinguishable from each other and
  from the vase's highlights, and no head pixel appears below the mouth outside the opening;
- unchanged shadow mask, additive-only silhouette, detail omission confined to the flower,
  diagnostic passes that never contaminate a normal render, five output sizes from 64 × 64 to
  4096 × 1024;
- desktop: both prompts through `GenerationService` → `DesktopSession` → save metadata, rejected
  daisy prompts that keep the previous preview and its seed/variation, three genuinely different
  images for the same vase (empty, tulip, daisy), and the two new filenames whitelisted with every
  earlier token unchanged.

## Artifacts

Everything lands in `artifacts/bin/Formloom.Tests/**`, which the existing `procedural-preview`
upload already globs — no workflow step was added.

| file | when |
| --- | --- |
| `formloom-vasedaisy-sheet.png` | every run — 320 px cells, 4 columns, 2 rows, row-major, no text in cells (the CI image has no font stack) |
| `formloom-vasedaisy-sheet.timing.json` | every run — the authoritative cell → case key |
| `formloom-vasedaisy-white-blue-seed-42-variation-0.35.png` (+ `-silhouette.png`, `.timing.json`) | every run — the canonical case |
| the other seven cases' PNG/silhouette/sidecar | `FORMFLOOM_EXPORT_ALL=1` |

Cell identity, row-major: **0** `white-blue` 42 · 0.35 · **1** `white-cream` 42 · 0.35 ·
**2** `white-blue-altseed` 1337 · 0.35 · **3** `white-cream-altseed` 20261105 · 0.35 ·
**4** `white-blue-lowvariation` 42 · 0 · **5** `white-cream-highvariation` 42 · 1 ·
**6** `white-blue-highvariation` 1337 · 1 · **7** `white-cream-lowvariation` 20261105 · 0.
The manifest carries, per cell, the name, prompt, seed, variation, subject token, appearance,
glaze, petal and leaf counts, the full-size and silhouette file names, the cell coordinates,
dimensions and the cell's render-and-encode time.

## Timing observations

Each sidecar records three Stopwatch samples from one runner instance: the daisy's
render-and-encode time, the same empty vase's, and the same tulip in the same glaze, plus both
deltas. The canonical file is
`formloom-vasedaisy-white-blue-seed-42-variation-0.35.timing.json` in the `procedural-preview`
artifact of run `42382836401`.

**No numbers are quoted in this document.** The authoring sandbox could not download the artifact
(every `gh run download` and log fetch returns `EOF`), so the sidecar was never read out of the
run; writing figures from memory or from another run would be fabrication. Open the canonical
sidecar and read the pair directly. Treat whatever it says as a single-sample observation from
that environment, exactly as the Phase 7 read-back is treated — the daisy adds one clipped stem
stroke, two leaf fills, twelve petal fills and one disk fill to the empty vase, so a large
difference would be noise, not a cost model.

## Visual review

**Recorded as partially passed, with one defect found and fixed.** The canonical case
(`formloom-vasedaisy-white-blue-seed-42-variation-0.35.png`, blue vase, seed 42, variation 0.35)
was reviewed at full resolution by the repository owner, who supplied the PNG for inspection. The
review was also checked against 4×/5×/6× crops of the head, of the stem and leaves, and of the
mouth.

Passed on that image:

1. **Reads as a daisy immediately** — white petals radiating around a distinct golden centre; not a
   sunflower, star, gear, pinwheel or tulip. The golden disk is domed, with a lighter crown and a
   darker ring, and it covers the petal roots, so no petal looks detached.
2. **Petal separation** — the twelve petals read as separate overlapping shapes with restrained
   seams between neighbours, not identical spokes and not one scalloped outline.
3. **The stem enters the vase correctly** — it crosses the far rim, is carried into the dark cavity,
   and disappears behind the near lip; no band across it, no stub, no exterior-wall leak, no gap at
   the tangent. This is the tulip's own tested treatment, unchanged.
4. **Vase and tabletop unchanged** — rim, mouth, body, glaze, foot and the contact shadow are the
   empty vase.

Failed, and fixed:

5. **Only one leaf was visible.** The geometry had its two leaves, but the upper one attached inside
   the head's vertical band (0.72 of the visible stem, in a stem only ~0.06 tall between the head
   and the rim) and the petals swallowed it, so the flower read as having a single leaf. The pixel
   test that was supposed to cover this counted *changed* pixels in a window around the leaf — and
   a leaf hidden behind a petal is covered by daisy ink too, so it passed vacuously.

   The fix derives the leaf band from the **head's own bottom edge** instead of a fraction of the
   stem: `leafBandTop = headBottom + 0.22 × headRadius`, `leafBandBottom = rimTop − 0.18 ×
   stemVisible`, with the two leaves placed at the bottom of that band and 45 % up it. The blades
   are longer (`min(0.72 × stemVisible, 2.3 × mouthRadius.X)`) and splayed almost horizontally
   (±~1.37 rad), so both sit in the clear stem and both reach past the head's silhouette. Two
   invariants now guard it: a **pixel** one counting green blade pixels in each leaf's own window
   (a covered leaf scores zero), and a **geometric** one requiring every leaf tip to lie below the
   bloom's bottom edge or beyond its side. The vacuous "changed pixels" leaf check is gone.

Cosmetic observations, recorded separately and **not** changed:

- The twelve petals are fairly even in length, which gives the head a slightly emblem-like
  symmetry. The per-petal jitter is deliberate but small.
- The head is modest against the vase — about a tenth of the canvas — because `headroom × 0.34`
  caps it. Enlarging it means spending the top margin, which is a different design decision, not a
  defect.

Failed, and fixed — the second defect, found in the group sheet:

6. **The stem vanished at the mouth in the 320 px sheet cells.** In every cell of
   `formloom-vasedaisy-sheet.png` the stem stopped dead at the rim and the cavity stayed empty, so
   the flower looked stuck into the lip — the same symptom on the tulip sheet
   (`formloom-vaseflower-sheet-2.png`). Measured on cell 0 with a green-ink test
   (`g > 75 && g > r + 12 && g > b + 12`) the stem contributed **8 pixels in 4 rows**, all above
   the rim; the same measurement on the full-resolution tulip
   (`formloom-vaseflower-red-blue-seed-42-variation-0.35-3.png`, 1024 px, crop `34x30+492+212`)
   found **215 green pixels** running through the cavity to the near lip. The defect was therefore
   present at 320 and absent at 1024 — a scale dependence, not a geometry error, and it affected
   every flower family because they share the pass.

   The cause is a fixed-space transform left over from an earlier draw step: the mouth
   continuation reset the canvas to identity but then scaled it by a constant 1024 instead of the
   scene's own size. At 1024 that is invisible; at 320 the whole pass draws 3.2× too far out and
   falls off the canvas. The pass now scales by `scene.Width` / `scene.Height`, the same way the
   scene's initial transform does.

   Three guards now hold this open at the sheet's own size, because the existing size matrix
   started at 640 and never sampled 320:

   - 320 × 320 joined the supported-size matrix, so the per-size bounds, silhouette and
     stem-inside-opening assertions all run at the cell size;
   - the sheet test scans the **composited sheet** cell by cell and requires green ink inside each
     cell's own opening — the artifact itself is checked, not just a render of the same size;
   - the tulip has the same check at 320 px for all eight of its cases, since it regressed there too.

   Both pixel checks test green **dominance** rather than a brightness floor: inside the cavity
   the stem is darkened by the depth gradient and thinned by antialiasing to barely a pixel at 320.

**The sheets that exposed both defects predate them.** The daisy sheet shows a single leaf in all
eight cells — 86–88 green blade pixels to the left of the stem in each cell and **zero** to the
right — which is the pre-fix geometry, so it is stale for the leaf question *and* was produced by
the build that still had the fixed 1024 transform. It remains valid as the reason the two defects
were found, and the fixes are proven by the tests above rather than by that image.

### The group sheets re-reviewed on the fixed build — passed

`formloom-vasedaisy-sheet-2.png` and `formloom-vaseflower-sheet-3.png` were rendered by the merged
build (run `42450438121`, merge `e738f50`) and reviewed cell by cell. Both defects are gone, and the
three review items that depended on the sheet are closed:

7. **Cream and blue glazes** — the white daisy with its golden disk reads against both. On the cream
   vases the petals separate from the rim because the head sits above it, and the warm brown
   interior keeps the green stem and both blades legible inside the mouth, where the blue vases use
   the dark navy cavity. The tulips hold the same way: red and yellow against cream and against blue.
8. **Alternate seeds and variation extremes** — seeds 1337 and 20261105, and variation 0 and 1, in
   both glazes: twelve petals every time, no broken or detached petal, no gap in the ring, both
   leaves attached on opposite sides, no clipping, no canvas-edge collision. The highest head tip in
   the sheets keeps a margin of about 8 px inside its 320 px cell.
9. **Legibility at 320 px** — every cell reads immediately as a daisy or a tulip; the golden disk
   and the twelve petal notches survive a head that is about a tenth of the canvas.

Measured on the same sheets, with green ink defined as `g > r + 8 && g > b + 14 && g > 45` so that
the dark teal backdrop does not count:

| check | daisy sheet | tulip sheet |
| --- | --- | --- |
| cells showing two blades (ink left / right of the stem) | 8 / 8 — 94–113 px left, 81–95 px right | 8 / 8 — 41–56 px left, 42–55 px right |
| green ink inside the mouth, per cell | 12–35 px | 21–46 px |
| green ink on the lip or body below the mouth | 0 px in every cell | 0 px in every cell |

The cream cells show less in-mouth ink than the blue ones (12–20 against 29–35) because the same
green is blended against a lighter interior; the stem is visibly there in all of them.

Cosmetic observations from this pass, recorded and **not** acted on:

- At **variation 0** the stem between the bloom and the rim is short, so the two blades sit close to
  the petals' underside and to the rim — cramped, not intersecting, and the honest consequence of a
  small flower in a tall vase.
- At **variation 1** the mouth tilts and the white near lip thickens; the stem still crosses the
  cavity and disappears behind the lip. That is the vase profile's own variation, inherited from
  Phase 7, not a flower defect.

Still open: the **timing observations**, which need the artifact's timing sidecar read back — the
daisy figures have not been transcribed — and any constant tuning the owner wants after looking at
the sheets.

## Known limitations

- One species of daisy, one appearance (white petals, golden centre), one bloom, one stem, two
  leaves, one vase per scene, and no arrangement control beyond seed and variation.
- The petal count, headroom share, squash, disk share, stem slenderness and leaf positions are
  authored constants tuned without visual feedback in this environment. A cosmetic pass after
  review may want different values; that is a constant change, not a structural one.
- The head is a flat illustrated disc: no stamen detail beyond the dome and ring, no translucency,
  no droop, no wind, no growth, no shadow of its own.
- Silhouettes include the head and shadow masks omit the flower — both are the documented
  conventions inherited from the earlier phases, not defects.
- Flower height is capped by the vase's headroom, so the plant is proportioned to the vessel
  rather than to the canvas.
- The daisy is not a general botany path: a third species needs its own typed content and its own
  material, and adding one is deliberately not a configuration change.

## Verification

- PR [#45](https://github.com/Vraxo/Formloom/pull/45) — model, geometry, grammar, validation,
  renderer, first test pass. CI run
  [42378487136](https://github.com/Vraxo/Formloom/actions/runs/42378487136) green on head
  `398feac` (earlier runs on the same PR: `42377583480` failed on CA1822, `42377671588` and
  `42377964737` failed on two of this milestone's own pixel scans, `42378100174` and
  `42378232262` on the leak scan's units, `42378359262` green with the temporary
  failing-test annotation step still in place). Merged as `bfa9bdb`; post-merge run
  [42380161903](https://github.com/Vraxo/Formloom/actions/runs/42380161903) green.
- PR [#46](https://github.com/Vraxo/Formloom/pull/46) — artifacts, silhouettes, timings and the
  review sheet. CI run
  [42382836401](https://github.com/Vraxo/Formloom/actions/runs/42382836401) green on the first
  attempt; `procedural-preview` 3.26 MB, `test-results-Release` 125 KB. Merged as `a5b8cae`;
  post-merge run [42384279037](https://github.com/Vraxo/Formloom/actions/runs/42384279037) green.
- PR [#47](https://github.com/Vraxo/Formloom/pull/47) — desktop/component coverage, the two extra
  pixel properties, README, this document, and the Phase 7 correction. The run sequence is worth
  recording because the tests earned it: [42385310068](https://github.com/Vraxo/Formloom/actions/runs/42385310068)
  failed at the per-size bounds property, [42385431384](https://github.com/Vraxo/Formloom/actions/runs/42385431384)
  reported the first offenders, [42385571746](https://github.com/Vraxo/Formloom/actions/runs/42385571746)
  failed to compile the diagnostic string,
  [42385649123](https://github.com/Vraxo/Formloom/actions/runs/42385649123) produced the coordinates
  that identified the fixed-matrix continuation bug described above, and
  [42385814291](https://github.com/Vraxo/Formloom/actions/runs/42385814291) was green with the fix.
  Run [42385997594](https://github.com/Vraxo/Formloom/actions/runs/42385997594) is the green run on
  the shipping code state (the temporary failed-test annotation step already removed); the
  documentation commit that follows it is verified by the run on the PR's final head, which is
  named in the PR conversation.
- **Not claimed:** native Photino/WebView2 dialog, window lifecycle, resize or accessibility
  behaviour. Those are not exercised by headless tests or Linux CI, in this milestone or any
  earlier one.
