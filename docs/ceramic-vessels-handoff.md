# Phase 4 — ceramic vessels and mixed still-life scenes

## Status and starting point

Starting `main` commit: `259ad6c` ("todo 4"), working tree clean, no local .NET SDK, no branch switching, no dependency, project or workflow changes.

Phase 3 was complete at `b9771a8` (PR #25): fruit-only composition with one to three instances, a strict parser, deterministic layout and targeted overlap/shadow invariants. Its handoff is [composition-handoff.md](composition-handoff.md).

**This document records the implementation, the automated evidence and the visual review of the generated ceramic images**, supplied as the 1024 × 768 review sheet on 2026-11-05.

## Smallest extension that fit the existing architecture

The Phase 3 path was `PromptParser` → `SceneGenerator` → `FruitComposition` → `StillLifeRenderer` → PNG/subject metadata → `DesktopSession`/`PngSaveService`. Vessels reuse all of it:

- **Core gained typed data, not a framework.** `CeramicKind`/`CeramicGlaze` (closed enums), `CeramicPrompt`, `CeramicDescription` (center, size, surface variation), `CeramicInstance` (kind, glaze, body, seed, depth) and one normalized `CeramicProfile` per kind. No `object`, no reflection, no type registry.
- **The scene stays explicit.** `StillLifeScene.Instances` (fruit) is untouched; a second optional `Entries` collection carries mixed scenes in prompt order. `SceneEntry` is a two-case tagged projection (fruit or ceramic) that also supplies bounds, ground offset and horizontal shift. `Objects` merges the legacy single fruit, `Instances`, or `Entries`, so every existing caller keeps working; `Fruits` and `Ceramics` are filtered views.
- **Layout got one sibling.** `FruitComposition` is untouched for fruit-only scenes, so their placements, random-stream consumption and pixels are preserved. `MixedComposition` arranges ceramic-only and mixed scenes with the same `scene.composition` stream, four slots-orientated placements, per-kind ground offsets and a horizontal shift that centres the visible silhouette instead of the profile origin.
- **Geometry is shared where it fits.** `CurveBuilder.ClosedCubic` now owns the closed cubic smoothing; `OrganicForms` calls it with byte-identical math, and the vessel outlines use it with their own point lists. Vessels deliberately do **not** reuse `OrganicContour` semantics (no stem attachment, no asymmetry), because forcing a vessel through fruit semantics would be the wrong abstraction.
- **Rendering stayed one pass structure.** `RenderCore` iterates `scene.Objects` by depth and dispatches to the existing fruit draw or the new ceramic draw; groups draw every ground shadow first. No second pipeline, no new canvas, no static caches.

## Vessel profiles

Each profile is normalized in the fruit contour space (0..1, y down) and carries the closed exterior outline plus the opening data the renderer needs:

| Vessel | Silhouette | Mouth | Cavity | Base | Handle |
| --- | --- | --- | --- | --- | --- |
| Bowl | wide, shallow, small foot | wide ellipse near the rim | deep, concave | narrow foot, 0.24 of width | none |
| Cup | tapered cylinder, taller than wide | moderate ellipse at the top | shallow | broad, 0.46 of width | ring behind the body, open centre |
| Vase | base, broad body, shoulders, narrow neck, flared mouth | small ellipse, 0.28 wide vs a 0.68 body | shallow | 0.37 of width | none |

Semantic invariants are asserted, not just enum identity: the bowl's mouth is wider than the vessel is tall, the cup's mouth is 0.6–0.9 of its height with a third of the handle ring open, and the vase's body is more than twice its mouth width. `CeramicBounds` samples the outline and unions the mouth ellipse and handle, so validation and layout never see a cropped handle.

## Rendering approach

1. Ground shadows first for the whole group (blurred oval plus two contact ovals, scaled by body width, offset opposite the light direction).
2. Per object, back to front. A cup handle ring is drawn **behind** the body so the joins are covered, with the inner ellipse traced in reverse so non-zero winding leaves a real hole; through it the table stays visible.
3. Ceramic body fill: radial glaze gradient (light upper-left, dark lower-right), then inside the body clip the glaze mottle, the mouth, and a specular highlight, then a thin darker outline.
4. Mouth: a cavity gradient darker than the body, a soft far-wall sheen, a deeper floor ellipse, and the front lip drawn as a thicker lower arc — this is what makes the bowl read as concave rather than a painted oval.
5. Complete silhouettes fill the outline and the handle ring black and keep the handle hole transparent. Vessel mouths are interior surfaces, not through-holes, so they are opaque in a silhouette; that convention is documented here and asserted by the silhouette test.
6. Glaze and geometry are independent: `CeramicMaterial` derives blue/cream palettes plus a seeded tone shift from `ceramic.{kind}.glaze`, while mottle detail uses `ceramic.{kind}.glaze.detail` and is scaled by `SurfaceDetail`, so `RenderWithoutInstanceDetail` has a real, clipped detail channel to suppress.

## Grammar and metadata

`PromptParser.ParseObjects` accepts the six fruit descriptors and six vessel descriptors in the existing one-to-three list syntax with the same normalization. `ParseScene`, `ParseFruit` and `Parse` stay fruit-only, so Phase 2/3 callers and tests keep their contract; a ceramic prompt is rejected by those APIs and accepted by `ParseObjects`.

Subject tokens: single fruit keeps its species token, a fruit group keeps `fruit-group`, a single vessel is `ceramic-bowl`/`ceramic-cup`/`ceramic-vase`, a ceramic-only group is `ceramic-group` and a mixed group is `still-life-group`. `PngSaveService` whitelists those tokens and still falls back to `fruit` for anything else, so raw prompt text can never become a filename.

## Tests added

- **Core/profiles**: closure and continuity of every outline, finite bounded control points, semantic proportion invariants, bounds including handle and contact line, unknown kind/glaze/depth, non-finite and out-of-range transforms, empty/default/excess collections.
- **Grammar**: all six descriptors, single/pair/triple forms in every order, repeats, Oxford comma, normalization, and seventeen rejected phrasings (missing `ceramic`, wrong colour, `plate`/`mug`, extra adjectives, spatial prepositions, four objects, `or`, bare commas).
- **Composition**: single vessel centred and grounded across variations; 40 seeds × 3 variations × 7 mixed prompts with valid margins, grounding and separation; determinism; seed-driven permutation; fruit-only compatibility path; duplicate-vessel seeds; stream stability when a different object is added or reordered.
- **Renderer**: six glazed vessels at 1024², four output sizes, repeatability, seed response, glaze distinction, bowl cavity gradient vs front wall, cup handle hole openness and no gap between ring and body, vase mouth vs lip vs shoulder, silhouette borders, detail containment inside each object's own silhouette, two-directional depth occlusion with a real overlap count, and vessel/mixed shadow masks (contact alpha, right-biased cast extent, transparent canvas borders) across seeds 0/42/1337 and variation 0/1. Diagnostic passes are re-checked for byte-identical normal output.
- **Desktop**: real `GenerationService`/`DesktopSession` runs for single vessels, ceramic-only pairs and mixed scenes, failure retention with an unsupported ceramic prompt, submitted prompts through the rendered Razor component, and filename/metadata snapshots.

All existing fruit, fruit-group, shadow, clipping, PNG, resource-ownership and repeatability tests are retained and pass unmodified.

## CI evidence

[PR #26](https://github.com/Vraxo/Formloom/pull/26) is the phase PR, merged as `8a83391`; the post-merge Linux Release run [42006648475](https://github.com/Vraxo/Formloom/actions/runs/42006648475) passed at that commit. `Linux / Release` runs restore, the warnings-as-errors build of the whole solution, both test projects and both artifact uploads.

- First run failed on an obsolete `SKCanvas.DrawBitmap` overload (warnings as errors) and then on `MoveToImmutable` capacity in `PromptParser`; both were fixed in-branch.
- One test-only assertion conflated layout scaling with stream stability and was corrected.
- Run [42006254568](https://github.com/Vraxo/Formloom/actions/runs/42006254568) is green at `599284d`, the final code-complete commit, with the complete suite, including every pixel invariant and both artifact uploads. An earlier green run, [42005432452](https://github.com/Vraxo/Formloom/actions/runs/42005432452) at `cd087de`, carried the same suite while the temporary diagnostic step was still present.

Because this sandbox has no .NET SDK and GitHub's redirected artifact/log storage is unreachable, failures were read through the check-run annotations API. A temporary workflow step was used for that during development and then removed; the committed workflow is unchanged apart from nothing (no new jobs, runners or packages).

## Generated review artifacts

The existing `procedural-preview` artifact now also contains **33 ceramic PNGs** and 16 timing sidecars from `ExportCeramicScenesTimingsAndSilhouettes`:

- six single vessels (both glazes), three mixed pairs (`apple-bowl`, `pear-cup`, `orange-vase`), a bowl+cup ceramic pair, two mixed trios, seed 1337 bowl+cup, and variation 0/1 extremes for `apple-bowl` and `vessel-trio`, each with a matching transparent complete-subject silhouette;
- `formloom-ceramic-sheet.png`, a compact 4-column review sheet (256 px cells, no text labels to avoid a font dependency).

Sheet cell order (left to right, top to bottom): blue bowl, cream bowl, blue cup, cream cup / blue vase, cream vase, apple+blue bowl, green pear+cream cup / orange+blue vase, apple+cream bowl+orange trio, cream cup+pear+blue vase trio, blue vase+blue bowl.



## Visual review — supplied review sheet, 2026-11-05

The user supplied the rendered sheet (`formloom-ceramic-sheet.png`, 1024 × 768, twelve 256 px cells in the order listed above) plus a listing of the artifact contents. Each row was inspected at 1× and again at 2× magnification; the notes below are what the pixels show, not an inference from the tests.

**Bowl (blue, cream)** — the strongest of the three. The interior reads as genuinely concave: a dark cavity with a lighter far-wall sheen and a brighter front lip along the near rim, over a wide shallow body with a visible foot. Both glazes show a soft upper-left specular and a smooth falloff, and the contact shadow sits directly under the base. Unmistakably a bowl.

**Cup (blue, cream)** — a tapered handled vessel with a dark mouth ellipse at the top, a thin light rim and a D-shaped ring handle on the right whose centre is genuinely open (the background shows through). The ring joins the body cleanly at both attachment points. Honest caveat: it reads closer to a **mug/tankard** than a teacup, and the interior is a shallow dark ellipse with only a faint inner wall — least legible of the three openings, particularly in blue.

**Vase (blue, cream)** — base, body, shoulder, long neck and a small mouth with a light rim. Reads as a slim **bud vase / bottle**: the neck is long and the mouth flare is subtle, so the "bottle" reading is the second-strongest cue, but the vessel identity is clear and the mouth is visibly open.

**Pairs** — apple + blue bowl: plausible scale (the bowl is a little wider than the apple), clear separation, both grounded with attached contact shadows and consistent upper-left lighting. Pear + cream cup: coherent, though the cup reads small (espresso-sized) beside the pear. Orange + blue vase: natural height contrast, both grounded.

**Trios** — apple + cream bowl + orange: the bowl anchors the foreground left, the apple sits smaller and higher as the rear object, the orange closes the right; all three stay readable and nothing is hidden. Cup + pear + blue vase: the rear cup is correctly smaller, the pear and vase define the foreground, and the cup's handle stays visible.

**Ceramic pair** — blue vase + blue bowl: clear silhouette separation and consistent grounding.

**Cross-cutting** — no detached contact shadows, no canvas clipping, no missing stems/leaves/calyces or handle joins; no speckle or pore-like noise on glaze; blue and cream both read as glazed ceramic and are clearly distinct from fruit skin and from the wood. Depth order in the trios is legible.

**Scope of this review** — the twelve supplied normal renders were inspected. The individual 1024 px PNGs and the transparent complete-subject silhouettes in the artifact were **not** inspected directly; their structure is covered by the automated border/inclusion tests. This is a visual judgement of the supplied sheet, not a golden-image or historical comparison.

**Verdict: pass**, with the cup's mug-like proportion and the vase's bottle-like neck recorded as cosmetic character, not defects. No renderer fix is indicated by this sheet.

## Timing observations

Instrumented 1024 × 1024 render plus PNG encoding, four logical processors, Ubuntu runner (.NET 10):

| Case | Objects | Observed |
| --- | ---: | ---: |
| Single vessels (six glazes, seed 42) | 1 | 119.16–129.42 ms |
| Mixed pairs (apple+bowl, pear+cup, orange+vase, bowl+cup) | 2 | 129.35–136.46 ms |
| Mixed trios (two prompts, plus variation 1) | 3 | 133.24–142.68 ms |

Single samples from one CI run, not a latency distribution or a desktop guarantee; no peak-memory measurement is claimed.

## Pending gates and limits

- The supplied review sheet passed inspection (see above). The individual full-resolution PNGs and silhouettes in the artifact remain available for a closer look if a later change touches ceramic rendering.
- The arrangement remains a conservative slot layout. Prompts cannot express left/right/behind, and no manual placement, containment, stacking, liquid or flowers exist.
- Same-runtime determinism only; no cross-driver or historical pixel-identity guarantee. The fruit-only pixel behaviour is preserved by leaving `FruitComposition` and the fruit draw path untouched, and the existing regression tests still pass.
- No native Photino/WebView2 dialog, window-lifecycle, resize or accessibility behaviour is tested; desktop coverage is headless.
- Timing is advisory, from one shared CI run.

Stop at the Phase 4 acceptance checkpoint. No fifth object family, prop or grammar expansion is started.
