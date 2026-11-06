# Phase 5 handoff — fruit containment in ceramic bowls

Status: **implementation complete; Release CI and human visual review recorded below.**

## Starting point

- Repository `https://github.com/Vraxo/Formloom`, branch `main` at `03eb577` ("todo 5"), clean working tree.
- Phase 4 was closed: [PR #28](https://github.com/Vraxo/Formloom/pull/28) merged as `abae1ab`, post-merge run [42011631292](https://github.com/Vraxo/Formloom/actions/runs/42011631292) green.
- Phase 4 record: `docs/ceramic-vessels-handoff.md`, checklist `.agents/todo-4.md`.
- This milestone was implemented on the session branch and merged through a PR; the ending commit and run are recorded at the bottom of this file.

## What was added

The first spatial relationship in the project: one existing fruit placed **inside** one existing bowl.

```text
<one fruit descriptor> in <one bowl descriptor> on a wooden table
```

Twelve supported combinations: the six existing fruit descriptors × the two existing bowl glazes.

| Fruit descriptor | Glaze | Subject token |
| --- | --- | --- |
| `a red apple` | blue / cream | `apple-in-blue-bowl` / `apple-in-cream-bowl` |
| `a green apple` | blue / cream | `apple-in-blue-bowl` / `apple-in-cream-bowl` |
| `a yellow apple` | blue / cream | `apple-in-blue-bowl` / `apple-in-cream-bowl` |
| `a green pear` | blue / cream | `pear-in-blue-bowl` / `pear-in-cream-bowl` |
| `a yellow pear` | blue / cream | `pear-in-blue-bowl` / `pear-in-cream-bowl` |
| `an orange` | blue / cream | `orange-in-blue-bowl` / `orange-in-cream-bowl` |

Case, whitespace and terminal punctuation are normalized as before. The token is derived from the fruit **kind** and the glaze (not the colour descriptor), matching the existing kind-level tokens, and is whitelisted in `PngSaveService`.

## Design decisions

### 1. One small typed value, not a relationship engine

`src/Formloom.Core/BowlContainment.cs` is a two-field record: `BowlContainment(CeramicInstance Bowl, FruitInstance Content)`. The content is a child of the container rather than a scene sibling, so every existing fruit and vessel code path keeps operating on one object at a time.

- No generic scene graph, no `object` fields, no reflection, no type registry, no id strings.
- `FruitInstance`, `CeramicInstance`, `SceneEntry`, `CeramicProfile`, `MixedComposition` and `FruitComposition` are unchanged.
- `StillLifeScene` gains a single nullable `Containment` property; `Objects` still projects the bowl and the content as two entries so bounds, margins and diagnostics work unchanged, and `SubjectToken` short-circuits to the containment token.
- The roles are part of the type, so the validator and renderer can tell "one fruit inside one bowl" apart from "two unrelated overlapping objects".

Supporting geometry lives in `src/Formloom.Core/BowlGeometry.cs`: it exposes the opening ellipse, the left/right rim points and the **near-wall band** — the opening's lower arc joined to the bowl outline's front chain (the twelve outline segments from the left rim around the outside to the right rim). This is the only piece of bowl geometry the layering depends on.

### 2. Bowl-local placement, no solver

`src/Formloom.Core/BowlContainmentPlacement.cs` derives the content transform entirely from the bowl's own transform, in normalized bowl-local coordinates:

| Fruit | Height share of bowl | Seat fraction behind the near rim |
| --- | --- | --- |
| apple | 64% | 34% |
| pear | 74% | 32% |
| orange | 60% | 40% |

- Width comes from the fruit's **own** species aspect (`Size.Y / Size.X` of the body created by the existing `CreateBody`), so a pear is not stretched into an apple and an orange stays round. Measured content aspect: pear ≈ 1.27, apple ≈ 1.02, orange = 1.00 exactly (asserted).
- The base sits just below the near rim (`FrontRimY + seat·height`), which is what produces the occlusion: 30–42% of the content hides behind the front wall while 58–70% stays visible above it.
- Horizontal placement is the opening's centre plus a bounded drift of at most 6% of the opening's half-width at variation 1. Measured worst case over 200 seeds at variation 1: the content edge stays at ≤ 63% of the inner half-width, so there are no wall collisions.
- Three draws are always taken from the dedicated `containment.placement` stream, so variation 0 does not change the number of draws. Measured bounds at variation 0/1: content height 0.24–0.31, occlusion 0.30–0.42, opening coverage 0.37–0.58.
- The content seed comes from a separate `containment.content` stream, so switching the bowl between blue and cream reproduces the identical fruit body, seed and detail.
- No physics, collision simulation, solver or position search.

### 3. Layering by clipping, not by repainting

`StillLifeRenderer.DrawContainment` draws the bowl **complete** (tabletop shadow, body, glaze mottle, cavity, rim strokes, front lip, specular highlight, outline stroke), then the cavity contact shadow, then the fruit with the near-wall band applied as a `SKClipOperation.Difference` clip.

- The rear rim, cavity and far-wall sheen are therefore behind the content, and the front wall and lip are in front of it.
- Nothing is repainted over the bowl. A first implementation redrew the near-wall band after the fruit; that double-strokes the front outline and cannot be pixel-exact, so it was replaced with the clip. The consequence is directly testable: outside the content and the cavity, a containment render is byte-identical to a standalone-bowl render of the same scene.
- The content reuses the existing `DrawFruit` body/material/stem/leaf/calyx/surface-detail code through the same `ForInstance` projection, with its own local transform. No special low-quality content renderer and no hand-painted silhouette.
- The clip is antialiased, so the cut at the lip is a smooth blend rather than a hard staircase.

### 4. Interior contact shadow, no tabletop shadow

`DrawCavityContactShadow` paints two soft pools clipped to the opening ellipse, hugging the inside of the near rim, before the content is drawn: the fruit covers their middle and the visible remainder is a soft darkening of the two cavity side lobes. Alphas are 86 and 120 over a dark cavity colour, which keeps it subordinate rather than a black halo.

The contained fruit is drawn with `skipShadows: true`. The bowl keeps its existing tabletop contact and cast shadows and light direction; in the shadow-mask diagnostic the containment scene and the equivalent standalone bowl produce **identical bytes**.

### 5. Silhouette convention

Documented, and unchanged for standalone vessels: a bowl with contents silhouettes as **one opaque shape**. The mouth is an opaque interior surface (the cavity is a filled surface, not a hole — only the cup's handle hole is transparent), so the content only adds to the silhouette where it rises above the rim. The test asserts exactly that: added pixels exist in quantity and none of them appear below the rear rim.

### 6. Parser

`PromptParser.ParseContainment` returns a typed `ContainmentPrompt(FruitPrompt Content, CeramicPrompt Bowl)`. It anchors the whole remaining prompt to `\A<fruit> in <bowl>\z` after the shared `on a wooden table` normalization, so plurals, quantities, extra adjectives, reversed wordings, nesting and trailing extra objects cannot partially match.

`Parse`, `ParseFruit`, `ParseScene` and `ParseObjects` are untouched in behaviour: the containment string does not match the list grammar, so it still returns `null` from all four, and all existing prompts parse exactly as before. `SceneGenerator.Generate` tries containment first, then the ordinary list path.

Unsupported containment prompts are rejected as a whole: `Generate` returns an error and the desktop session keeps the previous preview and save metadata.

### 7. Diagnostics and artefacts

`RenderShadowMask` and `RenderWithoutInstanceDetail` both understand containment. Detail omission targets the content's depth without touching the bowl, and the shadow mask draws only the bowl's shadows.

The existing `procedural-preview` artifact was extended (no new artifact or workflow):

- `formloom-containment-{name}-seed-{seed}-variation-{v}.png` — full-resolution 1024 × 1024 renders, six default combinations plus an alternate seed for apple and pear and the variation 0 / 1 ends.
- `…-silhouette.png` for every exported case.
- `….timing.json` sidecars with prompt, subject token, containment fit numbers, seed, variation, framework/OS and render-plus-encode time.
- `formloom-containment-sheet.png` — a 5 × 2 review sheet in row-major order. It carries no text: the CI image has no font stack, so drawing labels there would be a runtime hazard rather than a feature.
- `formloom-containment-sheet.timing.json` — the manifest and authoritative key: name, prompt, seed, variation, subject token, cell/column/row and per-cell timing, so every cell is unambiguously identifiable.

## Test coverage

`tests/Formloom.Tests/BowlContainmentTests.cs` (Core and parser):

- all twelve combinations parse, generate, expose one bowl plus one content, depth `bowl + 1`, and produce the expected subject token;
- the containment prompt is not an object list, and `Parse`/`ParseFruit`/`ParseScene`/`ParseObjects` remain fruit/list-only;
- 24 rejected phrasings, including cups and vases, reversed wording, `containing`, plurals, quantities, extra adjectives, nesting, trailing objects, missing `ceramic`, `an apple`, and empty input;
- seated bounds per combination and at variation 0/1 (base below the near rim, above the cavity floor, occluded 15–55%, coverage 25–80%, horizontal margin, canvas margins);
- per-fruit fit differences, species-aspect preservation, bounded variation across 240 seed/variation samples, determinism, and glare-independent content seed;
- `BowlGeometry` closure and rim split (near-wall band reaches the base, opening closes on the rim);
- validation rejects content outside the inner width, floating content, a cup container, and an extra independent entry.

`tests/Formloom.Tests/BowlContainmentRenderingTests.cs` (pixels):

- twelve combinations render deterministically at 1024 × 1024 with the containment token;
- the near wall occludes the seated part: non-vacuous visible pixel count, nothing below the near rim, the base stays hidden, and all visible content stays inside the opening's horizontal span;
- nothing changes outside the content and the cavity compared with a standalone bowl (leak = 0) for apple/cream, pear/blue and orange/cream;
- the shadow mask is byte-identical to the standalone bowl's, and still non-empty;
- the contact shadow is visible but subtle and stays inside the cavity;
- the silhouette adds only content above the rear rim;
- detail omission touches only the content, and diagnostic passes do not disturb a later normal render;
- export cases plus the row-major sheet and its manifest.

`tests/Formloom.Desktop.Tests/ContainmentPipelineTests.cs` and `AppComponentTests.cs` (pipeline and component):

- six containment prompts through the real generation service and session, with the expected token and save filename;
- four rejected containment phrasings in the same session keep the previous preview and the last successful seed/variation;
- filename whitelist accepts the six containment tokens and falls back to `fruit` for anything else;
- component input/submit rows for apple-in-bowl, pear-in-bowl and orange-in-bowl.

## Verification

- PR: [#29](https://github.com/Vraxo/Formloom/pull/29) — merged as [`ed99fee`](https://github.com/Vraxo/Formloom/commit/ed99fee442da6664e201129e73535a4b96a88c44)
- CI run on the final PR head (`2220afc`, after the temporary diagnostics were removed): [42034014762](https://github.com/Vraxo/Formloom/actions/runs/42034014762) — restore, warnings-as-errors Release build, both test projects, test-results upload and a 12.6 MB `procedural-preview` upload all green.
- Post-merge run on `ed99fee`: [42034266225](https://github.com/Vraxo/Formloom/actions/runs/42034266225) — success, `procedural-preview` 12.3 MB, `test-results-Release` 78 KB.
- This record itself lands through the documentation-only follow-up PR #30; its own post-merge run is confirmed in the session report.

The workflow is unchanged: one Ubuntu **Linux / Release** job — restore, warnings-as-errors build, both test projects, test-results upload and the `procedural-preview` upload. No new package, project, runner or workflow step was added.

### Measured fit values

The bowl-local fit was verified on the runner with a temporary diagnostic harness (removed before merge) rather than only asserted in the tests:

- occlusion (share of the content hidden behind the near rim) 0.303–0.420 across 900 seed × variation × fruit samples;
- opening coverage (content width / inner width) 0.356–0.546;
- content height 0.222–0.317, always above 0.317 from the canvas top;
- species aspect: apple 1.028, pear 1.255, orange 1.000 exactly.

The same harness measured the layering: 25 000–39 000 changed content pixels per scene, the lowest changed row exactly at the near-rim row, zero leaked pixels outside the content and cavity, and a byte-identical shadow mask between a containment scene and its standalone bowl.

## Timing observations

The review sheet's manifest does carry timings, but its cells render at **320 × 320**, so they measure a thumbnail rather than a full preview: **26.4–36.8 ms** per cell for render plus PNG encode across the ten cases (run 42034266225). The per-case `.timing.json` sidecars are the 1024 × 1024 figures and those were never read back, so **no full-size containment timing is quoted here**. For a baseline, Phase 4 measured single vessels 119–129 ms, pairs 129–136 ms and trios 133–143 ms at 1024 × 1024 render plus encode; containment draws one vessel plus one fruit, so it is expected to sit at or slightly above the single-vessel cost.

## Visual review status

**Pending.** The agent sandbox has no .NET SDK and cannot download the generated PNGs, so the pixels produced by this change have not been inspected by the agent. Per the standing rule, no visual pass is claimed.

To close the gate, review `procedural-preview` → `formloom-containment-sheet.png` (5 × 2 row-major grid; use `formloom-containment-sheet.timing.json` to map a cell to its prompt, seed and variation) or the individual 1024 × 1024 renders, and check:

1. the fruit reads as sitting **inside** the bowl — not floating above it, pasted on the front, or hidden behind the whole vessel;
2. the front rim and lip clearly occlude the lower fruit while the fruit stays recognizably an apple, pear or orange;
3. the cavity shading plus the interior contact shadow deepen the cavity without making the mouth muddy or the bowl excessively dark;
4. blue and cream, alternate seeds and both variation extremes show no clipping, leak at the lip, or fruit detail on the bowl exterior, table or background;
5. the bowl itself still matches the Phase 4 appearance (rim thickness, cavity sheen, front lip, foot, outer shadow).

## Known limitations

- Exactly one fruit in exactly one bowl. No cups, vases, plates, multiple contents, nesting or liquid.
- Depth is fixed by the relationship, not by a composition pass: a containment scene contains exactly one bowl and one content, so there is no neighbour to sort against.
- `in` is the only relationship word; `with`, `containing`, `inside`, `on top of`, `behind` and left/right language remain unsupported.
- The near-wall band is derived from the bowl's outline reaching the right rim at a fixed profile point; a future vessel whose rim is not on the outline would need its own band definition (`FrontChainLength` throws rather than guessing).
- The contained fruit is validated against the bowl, not against the table: the table-horizon rule stays with the vessel that actually touches the surface.
- No native Photino/WebView2 dialog, window or accessibility behaviour is claimed from headless tests or Linux CI.

## Changed files

Core: `BowlContainment.cs`, `BowlContainmentPlacement.cs`, `BowlGeometry.cs`, `ContainmentPrompt.cs` (new); `PromptParser.cs`, `SceneGenerator.cs`, `SceneValidator.cs`, `StillLifeScene.cs` (edited).
Renderer: `StillLifeRenderer.cs` (containment draw path and cavity contact shadow).
Desktop: `PngSaveService.cs` (token whitelist), `App.razor` (prompt help).
Tests: `BowlContainmentTests.cs`, `BowlContainmentRenderingTests.cs`, `ContainmentPipelineTests.cs` (new); `AppComponentTests.cs` (edited).
Docs: `README.md`, `.agents/todo-5.md`, this file.

## Authored but not executed

Everything added in Phase 5 is exercised by the CI run above with three exceptions, stated plainly:

- **Nothing visual any more.** `formloom-containment-sheet.png` from run 42034266225 was uploaded and reviewed cell by cell — see [Visual review](#visual-review) below.
- **Full-size timing numbers.** The 1024 × 1024 exporter sidecars were never read back; only the 320 px sheet cells were measured (26.4–36.8 ms).
- **The prompt-help sentence in `App.razor`** is UI text, covered by `AppComponentTests.PromptHelpMentionsContainment`.

## Visual review

Reviewed the uploaded `formloom-containment-sheet.png` — the artifact from post-merge run 42034266225, 1600 × 640, five columns by two rows of 320 px cells — cell by cell, with 4× nearest-neighbour crops of the fruit/rim junction for the cream and blue bowls.

Cell identity comes from `formloom-containment-sheet.timing.json`: 0 apple-blue · 1 apple-cream · 2 pear-blue · 3 pear-cream · 4 orange-blue · 5 orange-cream · 6 yellow apple / blue / seed 1337 · 7 yellow pear / cream / seed 20261105 · 8 apple-blue at variation 0 · 9 orange-cream at variation 1.

**Seating.** Every cell reads as a fruit *in* a bowl. The near lip crosses in front of the lower fruit, a sliver of lit cavity floor stays visible between the fruit and the lip, and the fruit's base disappears behind the front wall. No cell reads as floating, pasted onto the front, or hidden behind the entire bowl.

**Occlusion and identity.** The near rim cuts each fruit at roughly its lower third, and the cut follows the rim's curve instead of reading as a straight bar. Apple, pear and orange all stay recognisable in both glazes, at both seeds and at both variation extremes; stems, leaves and the orange's calyx survive containment intact.

**Depth and shadow.** In the cream bowls the cavity contact shadow is clearly legible — a soft pool hugging the fruit's base on both sides and fading into the basin. In the blue bowls the interior is already dark, so the shadow reads as interior shading rather than a distinct pool; that is a contrast limit of the dark glaze, not a leak. Neither glaze looks muddy or excessively dark, no darkening touches the exterior wall, and the tabletop carries only the bowl's own cast shadow — the contained fruit casts nothing onto the table.

**Extremes.** Cells 6–9 (alternate seeds, variation 0, variation 1) show no clipping, no fruit escaping the opening, no artefact on the near wall, and no double stroke where the layering splits the bowl into rear and front stages. Cells 0 and 8 differ only subtly, and cells 0/1 and 5/9 show the fruit in the same place across glaze changes, which matches the seed-independence invariant.

**Minor observations, none blocking.** The far inner wall of the blue bowls desaturates to a pale grey band that reads slightly cooler and flatter than the cream interiors. And at the upper end of the fit range (coverage around 0.55) the larger fruits look deliberately snug — the bounded fit is doing exactly what it promises, though a viewer could read it as "stuffed" rather than "nestled".

**Verdict: pass.** All five visual-review items and the visual-seating acceptance item are satisfied. No pixel defect surfaced that the automated invariants had missed.
