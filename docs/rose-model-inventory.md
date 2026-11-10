# Rose model inventory — data, algorithms, and renderer capabilities

## Purpose and status

This is an implementation-oriented audit of the legacy rose, written after the initial schema-v1 evaluator was merged. It is a migration planning document, not a frozen schema or a demand to reproduce every legacy class as JSON.

The governing boundary is [ADR-0001 — Keep Formloom a hybrid procedural engine](adr/0001-hybrid-procedural-engine.md):

- Model-specific facts, proportions, profiles, counts, palette values, and composition belong in external model data.
- Reusable mathematical and rendering capabilities belong in ordinary C#.
- A model definition may compose a finite set of typed capabilities; it must not become a general-purpose programming language.
- If a behavior is genuinely specialized and cannot be expressed clearly with reusable capabilities, stop and reassess rather than disguise a rose-specific method as a generic operator.

The source locations below refer to the legacy implementation retained on `main` after PR #63. The legacy implementation remains the behavioral and visual reference until a replacement is proven equivalent.

## 1. Model-specific knowledge that should move to data

| Existing knowledge | Legacy source | Intended data representation |
|---|---|---|
| Three bloom layers, counts 5 / 5 / 3, phase offsets 0 / 0.5 / 0.18, and their painter ordering | `RoseGeometry.AddLayer` | Three ordered layer declarations with count, phase, profile reference, and layer-specific parameters |
| Broad outer/middle petal contour and curled inner-whorl contour | `RoseGeometry.BroadPetal`, `CurledPetal` | Two named normalized profile templates |
| Serrated rose-leaf outline | `RoseGeometry.LeafBlade` | Named normalized leaf profile; the serration is shape data, not an algorithm |
| Petal root radius, length, width, curl, and fold landmarks | `RoseGeometry.CreatePetals`, `AddLayer`, `CreatePetal` | Per-layer dimensions and named profile landmarks/feature paths |
| Leaf count, attachment positions along the stem, opposing directions, relative lengths, and width ratio | `RoseGeometry.Create`, `CreateLeaf` | Two leaf part declarations with attachment and transform parameters |
| Bloom placement ratios, headroom share, top margin, opening-relative dimensions, and stem thickness ratio | `RoseGeometry.Create` | Model parameters and bounded frame-relative expressions, once their semantics are represented explicitly |
| Petal tone bases/ranges for outer, middle, and inner layers | `RoseGeometry.AddLayer` | Per-layer base tone and bounded variation range |
| Red and pink palette values; shared stem/leaf palette values | `RoseMaterial` | Named material variants and generic material-role values |
| Stem, leaf, petal, fold, and vein semantic roles; detail-vs-silhouette intent | Rose records and renderer | Part/material-role assignments and generic detail metadata |
| Rose prompt descriptors, accepted red/pink choices, and subject naming | `PromptParser.ParseVaseRose`, `VaseRose.SubjectToken` | Catalog prompt descriptors and declared appearance choices; scene grammar remains generic |

These are facts about this rose. A different flower should be able to declare different profiles, counts, proportions, palette values, and attachments without adding a model-specific production C# branch.

## 2. Reusable algorithms that should remain in C#

These are capabilities, not rose data. They should have explicit, independently tested contracts and be composed by definitions.

| Capability | Legacy use | Existing engine support / gap |
|---|---|---|
| Map a normalized profile into a directed 2D basis with scale and offset | `MapPetal`, `MapLeaf` | Profile mapping exists in the reusable geometry layer and evaluator; confirm its transform semantics cover the legacy basis exactly |
| Convert ordered profile points into closed cubic contours | Petal and leaf outline construction | Reusable profile-to-cubic conversion exists; compare its interpolation and closure behavior against the legacy `CurveBuilder.ClosedCubic` |
| Construct and preserve cubic paths | Stem, folds, veins | Cubic path evaluation exists; reusable path utilities remain ordinary C# |
| Evaluate a cubic at parameter (t), sample it, and find a nearest/projected point | Stem attachment placement and `DistanceToStem` | Curve projection exists; confirm it supports the intended attachment contract and deterministic tie behavior |
| Construct a stable orthonormal direction/tangent basis and transform vectors | Petal orientation and leaf orientation | Not yet an explicit evaluator operation; add only the smallest reusable capability required by the real rose fixture |
| Apply bounded seeded variation using stable named streams | Stem sway, leaf placement, layer jitter, petal shape/tone | Radial evaluator variation exists; rose needs multiple independently scoped streams and per-part scalar ranges without stream coupling |
| Compute conservative bounds and validate finite geometry | Rose plant bounds and scene validation | Generic geometry validation exists; mounted-model constraints and safe-frame checks are not yet generic |
| Order paths and parts deterministically | Layer order plus within-layer depth sort | Evaluator preserves declared output order; the legacy within-layer sort is data/composition behavior once the required ordering key is represented |

A formula can be model-specific even when it uses ordinary arithmetic. For example, the rose's chosen relationship between vase headroom and bloom dimensions is rose knowledge; the reusable ability to evaluate a bounded expression and map a profile is engine capability. Do not move the former into a hard-coded C# helper merely because it is currently written as a formula.

## 3. Rendering behavior: generic capability, not rose-specific drawing

The current rose renderer does more than fill each outline. The replacement needs to preserve these behaviors through a generic style/material and mounted-content rendering contract.

| Current behavior | Legacy source | Required generic capability |
|---|---|---|
| Fill closed paths with a material role and optional per-part tone | `DrawRoseLeaf`, `DrawRosePetal` | Generic path fill and material-slot/tone resolution |
| Layered petal base/light/shadow appearance | `RoseMaterial`, `DrawRosePetal` | A bounded set of generic fill/gradient/shading styles; not arbitrary shader scripts |
| Petal edge stroke and inner fold stroke with layer-sensitive alpha/width | `DrawRosePetal` | Generic stroke style, opacity, width scaling, cap, and optional detail pass |
| Leaf blade fill, edge treatment, and vein stroke | `DrawRoseLeaf` | Generic fill/stroke roles and optional detail geometry |
| Stem rendered as a continuous, multi-pass stroke | `DrawVaseRose`, shared stem drawing helpers | Generic stroked cubic path with supported layered stroke/material treatment |
| Vase painted before the complete plant; the stem crosses the far rim continuously | `DrawObject`, `DrawVaseRose` | Generic mounted-content draw ordering |
| Lower stem continuation drawn inside the vase opening and occluded by the near rim | `DrawStemExtensionInsideOpening` call from `DrawVaseRose` | Generic mount-frame clipping/occlusion behavior tied to the vessel opening; do not encode a rose-specific renderer branch |
| Silhouette mode renders full geometry as a solid mask | `DrawRoseLeaf`, `DrawRosePetal` | Generic silhouette pass over geometry |
| Detail omission removes folds/veins/edges without deleting the main silhouette | Rose draw helpers | Generic per-path/per-role detail classification |
| Rose itself has no independent tabletop shadow or scene depth entry | `VaseRose`, `StillLifeScene` | Generic mounted model remains content of its host object rather than becoming a separate scene object |

The current `ModelGeometryPath` already carries ordered path identity, material-slot key, detail flag, optional material tone, and landmarks. It does **not** yet define a complete material/style system, stroke semantics, renderer passes, or host-vessel occlusion. Do not treat material-slot strings as a substitute for those contracts.

## 4. Rose-specific validation that should become declarative constraints plus generic validators

The current `SceneValidator.ValidateVaseRose` checks both general geometry safety and rose composition invariants.

**General checks suitable for generic validation:**

- finite coordinates, positive dimensions, non-degenerate paths, valid bounds;
- path/landmark identity and reference validity;
- attachment targets exist and are in the declared host frame;
- mounted geometry stays within safe host/canvas bounds;
- material values are finite and within declared ranges.

**Rose facts that should be declared as model constraints:**

- exactly one continuous stem, two leaves, and thirteen petals;
- ordered layer membership of five outer, five middle, and three inner petals;
- each petal has an outline and one fold path;
- each leaf has an outline and vein path;
- two leaves attach at distinct positions and extend to opposite sides;
- bloom center aligns with the stem top and the bloom clears the vase rim;
- the stem enters through the vase opening and rises above the rim;
- the model remains within the rose's declared safe proportions.

The engine should provide generic constraint primitives (counts, required roles, attachment relationships, bounds, frame-relative clearances, ordering) rather than a `ValidateRose` method. Do not add a broad constraint language until the rose fixture demonstrates the exact minimal set.

## 5. Current evaluator coverage versus the rose's needs

The initial schema-v1 evaluator implements five node types:

- `path.cubic`
- `curve.project`
- `profile.map`
- `array.radial`
- `geometry.group`

It can resolve bounded numeric expressions, parameters, mount-frame metrics, and prior-node landmarks; produce ordered generic paths; preserve material-slot keys, detail flags, optional material tone, and named profile landmarks; and apply deterministic radial variation.

This is a real execution foundation, but it is not yet enough to represent the complete legacy rose faithfully. The following gaps must be tested against a concrete rose fixture before adding operations:

1. **Non-radial repeated/independent parts:** two leaves with distinct attachment points and directions, and three petal layers with different profiles and parameters.
2. **General profile transforms/deformation:** exact basis mapping and the legacy sinusoidal curl/bow behavior, if those cannot be expressed with existing operations without contorting the definition.
3. **Stable per-part variation streams:** independent stem, leaf, and per-layer variation matching legacy stream isolation.
4. **Cubic attachment and landmarks:** reusable curve parameterization/projection with named attachment points and stable semantics.
5. **Generic mounted constraints:** frame-relative opening fit, rim clearance, attachment, and safe bounds.
6. **Material/style rendering:** generic fills, gradients/shading, strokes, opacity, material tone, and detail passes.
7. **Host-vessel occlusion:** generic continuous stem rendering through the opening with the near rim occluding the lower continuation.
8. **Scene and prompt integration:** a catalog-resolved model mounted in the existing vase relationship without rose-specific parser, generator, scene, validator, or renderer dispatch.

Do not implement all these gaps preemptively. The next implementation step is to choose one concrete gap, write its contract and focused tests, then add one reusable C# capability or data field only if the test proves it is necessary.

## 6. Migration safety rules

- Keep `RoseGeometry`, `VaseRose`, rose-specific validation, and `DrawVaseRose` intact until the data-driven output passes structural, deterministic, and visual comparison.
- Use the Phase 10 canonical and robustness PNGs plus timing sidecars as the legacy baseline. Their existence is not proof of a new pipeline review.
- Compare geometry before pixels: layer counts/order, paths, attachment points, bounds, material roles, and seeded variation.
- Then compare full-color renders, silhouettes, detail omission, host-rim contact, and material variants.
- Preserve stable named random-stream isolation where practical. Any intentional visual change must be called out and reviewed.
- Never add a model-ID/species branch to the evaluator, generic validator, or generic renderer to make the rose fixture pass.
- Never encode arbitrary scripts, unbounded expressions, or renderer-specific Skia objects in model data.

## 7. Immediate next step

Build a rose fixture using only the current schema-v1 vocabulary as an **expressiveness probe**, not as a rushed migration. Record the first unsupported requirement and its smallest reusable contract. Do not add multiple speculative operators in one change. Keep the legacy rose path running in parallel until the replacement is proven.
