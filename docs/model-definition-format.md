# Model definition format — design draft 0.1

> **Status:** Provisional design for Phase 11. This is not yet a runtime-supported format or frozen schema. The shape must be exercised by a runnable rose prototype before exact field names and operator contracts are finalized.

## 1. Design goal

**Architecture guardrail:** This format is a declarative description of model knowledge, not a replacement for the procedural engine. Reusable algorithms remain implemented in C#. Model files select profiles, counts, parameters, attachments, ordering, and appearance values and compose supported operations. Do not expand the format into a general-purpose language or encode an algorithm as a sprawling graph just to avoid C#.

A genuinely specialized model may retain a code-authored construction path when the data representation would be materially less clear or robust. See [ADR-0001: Keep Formloom a hybrid procedural engine](adr/0001-hybrid-procedural-engine.md), which governs operator design and model migration.

A model file describes how to construct an object by composing reusable engine operations. It must not be a bag of parameters passed into a species-specific C# generator.

The existing rose is the reference fixture. The format must eventually describe its broad/curled petal and serrated-leaf profiles; 5/5/3 radial petal layers; curved stem; two curve-attached leaves; veins and petal folds; vase-frame-derived dimensions; red/pink material variants; generic gradients/strokes; and stable per-part and per-element variation.

A second model must reuse the same operation vocabulary without the engine recognizing its model ID.

## 2. File and identity

Proposed extension: \`.model.json\`. Definitions are ordinary external files below a model root:

\`\`\`text
models/
└── flowers/
    ├── rose.model.json
    └── sunflower.model.json
\`\`\`

The proposed root fields are:

| Field | Purpose |
|---|---|
| \`format\`, \`schemaVersion\` | Document identity and integer schema version; initially version 1. |
| \`id\` | Stable namespaced ID such as \`flowers.rose\`, independent of filename. |
| \`name\`, \`category\` | Display name and broad catalog/prompt category. |
| \`prompt\` | Data-authored noun, aliases, article, and appearance descriptors. |
| \`parameters\` | Typed model parameters, permitted values, and ranges. |
| \`profiles\` | Reusable normalized path templates and interpolation settings. |
| \`materials\` | Renderer-neutral palette tokens and generic style definitions. |
| \`nodes\` | Typed construction/evaluation graph. |
| \`outputs\` | Named geometry roots and declared render order. |

IDs are stable across file renames, case-sensitive, namespaced, and unique in a catalog. Runtime catalog loading currently recognizes files ending in `.model.json`. Its default root is `AppContext.BaseDirectory/models`; callers and tests can pass any explicit root. Files are discovered recursively, and a second load takes a new snapshot rather than watching the directory. There is no multi-root override/merge policy yet. Prompt phrases are derived from the declared article, material-variant descriptor values, noun, and noun aliases; collisions reject all conflicting definitions.

The first structural contract is now represented by [`schemas/model-definition.v1.schema.json`](../schemas/model-definition.v1.schema.json) and the renderer-neutral records under `src/Formloom.Core/Models/Definitions/`. The schema and DTOs cover the initial metadata/profile/material-token/numeric-expression/node subset; they do not yet constitute a complete runnable rose format. Semantic reference checks and the final operator payloads remain implementation work.

## 3. Coordinates and attachment frames

### 3.1 Coordinate spaces

Use positive X to the right and positive Y downward, matching current Core geometry and ceramic profiles. Geometry must declare or inherit one of these spaces:

- **profile-local:** normalized template coordinates, normally in 0..1;
- **part-local:** a mapped profile in its oriented basis;
- **model-local:** the model's normalized construction frame;
- **scene-normalized:** current Core 0..1 scene coordinates.

Conversions are explicit operations. The renderer must never guess a point's coordinate space. A profile declares open/closed state, interpolation, and ordinary numeric point/control-point data. The profile interpolation modes are `linear`, `closedCubic`, and `openCubic`. `closedCubic` smooths a closed perimeter; `openCubic` smooths an ordered open path with endpoint-safe tangents for details such as veins and folds. Open paths may also use `linear` interpolation when straight segments are intended. Explicit `path.cubic` nodes remain available when a model needs authored Bézier control points.

### 3.2 Generic mounting frames

The scene relationship supplies a generic named frame; a model must not call a rose-specific vase helper.

The first frame contract is \`vase-opening\`, derived from the selected vase profile and instance transform. It exposes at minimum:

- opening center and X/Y radii in scene-normalized coordinates;
- mouth-top position and rim thickness;
- available growth/headroom bounds above the opening;
- an inset interior clip region for geometry continuing into the vessel.

The model may reference these values but cannot inspect a particular vase species. The relationship owns frame calculation and clipping; the model owns how it uses the available space.

The existing stem continuation into the vase should become generic mounted-stalk behavior, not a rose drawing method.

### 3.3 Profile mapping

Profile-map nodes may optionally declare bounded seeded variation for angle, length, width, and bend, together with a stable named stream. C# applies the ranges using the generation seed and variation amount. Related paths that must remain aligned (for example, a leaf outline and its vein) should use the same stream and identical variation ranges; independent parts should use distinct stream names.

The rose's petal and leaf contours use related basis mappings. Start with one reusable \`profile.map\` operation rather than rose-petal and rose-leaf functions.

Inputs should include profile, origin, oriented basis (direction/normal or angle), longitudinal/transverse scale, optional root offset, and optional bounded sinusoidal bend. It may expose named landmarks such as \`root\`, \`tip\`, and \`attachment\`. Mapping must transform contour points and curve controls consistently and preserve open/closed semantics.

## 4. Construction graph and operation candidates

The proposed \`nodes\` collection is a directed acyclic graph. Each node has a stable local ID, a typed operation discriminator, and operation-specific inputs. Nodes may reference only declared profiles, parameters, frame values, and earlier outputs. Cycles and unresolved references are invalid.

Candidate operations required by the rose:

| Operation ID | Responsibility |
|---|---|
| \`path.cubic\` | Create an open/closed path from cubic segments and point/anchor inputs. |
| \`curve.project\` | Find a point and tangent on a curve nearest to a target, for stable attachments. |
| \`profile.map\` | Map a normalized profile into a local oriented basis with scale and optional bend. |
| \`array.radial\` | Repeat child geometry around a center with count, phase, order, and per-element variation. |
| \`geometry.group\` | Group named parts while preserving explicit draw order. |
| \`geometry.stroke\` | Describe a generic path stroke, width, cap, and join. |
| \`geometry.bind-material\` | Bind a part to a material slot and its named landmarks. |

This list is provisional. It must change only when the rose or second-model proof demonstrates a real need. No operation may be named after a species, object, or model ID.

### 4.1 Numeric expressions

Some model geometry depends on simple relationships between frame measurements. The rose bloom radius, for example, is limited by both vase-opening width and available headroom. Reimplementing that \`min\` formula in C# would leave model-specific knowledge in the engine.

The draft therefore permits a **small typed numeric expression tree**, not a general scripting language. Initial operators: literal, declared parameter/frame/output reference, add, subtract, multiply, divide, min, max, and clamp. Expression graphs have strict depth/node limits and validate before evaluation. They cannot call functions, read files, use arbitrary conditions/loops, or execute user code.

If the rose can be expressed without this tree after prototyping, remove it. Nonlinear geometry such as profile bending belongs in a named generic operation with explicit inputs, not arbitrary expressions.

### 4.2 Repetition and determinism

A radial array declares count, start angle, phase, source geometry/profile, and bounded perturbations per element (angle, length, width, tone, as justified). Each node and repeated element receives a stable stream derived from generation seed and stable model/node/element identifiers. Adding or reordering unrelated parts must not alter existing output. Definitions declare ranges; the interpreter owns the PRNG and stream derivation.

## 5. Evaluated geometry output contract

The model-definition graph is an authoring/evaluation input; a renderer should not consume definition nodes directly. The interpreter emits a renderer-neutral ModelGeometry value containing:

- the stable model ID and declared coordinate space;
- an ordered immutable list of evaluated cubic paths;
- a unique path ID and source definition-node ID for each path;
- open/closed path semantics;
- an optional material-slot key and a detail-pass flag.

For the first implementation, emitted paths use **scene-normalized coordinates** (positive X right, positive Y down). Profile-local and part-local values are resolved by the evaluator before output. This deliberately gives the first renderer one unambiguous coordinate system; later local-transform support must be introduced as an explicit, versioned contract rather than inferred by the renderer.

The output list is flattened in draw order. The definition graph may retain groups and reusable source nodes, but a rendering backend should not need to understand definition-graph structure or group node types. Each evaluated path is a drawable leaf. The array order is the draw order and must be preserved.

Path bounds are conservative bounds of the cubic control hull, not a sampled estimate of the visible curve. They may be larger than the exact curve bounds, but they cannot exclude points on the cubic. Geometry validation rejects missing/duplicate IDs, empty paths, non-finite coordinates, disconnected adjacent segments, and closed paths whose final endpoint does not meet their first start. The validator does not impose a 0..1 bounds restriction: scene-normalized geometry may intentionally extend beyond the canvas and can be clipped by the scene/rendering layer.

This is the output contract only. It does not yet interpret ModelNodeDefinition, evaluate expressions, construct a rose, or render any geometry.

## 6. Materials and rendering

Definitions describe appearance without SkiaSharp types. A material variant contains named color tokens (petal base/light/shadow/edge/fold, stem/leaf base/light/edge, etc.). The first typed Core DTOs and schema represent these token maps. Renderer-neutral material slot/style definitions (fills, gradients, strokes, and detail bindings) still need to be finalized with the generic geometry output design. Parts will eventually bind to named material slots.

The generic style vocabulary should begin with only what the rose needs: solid fill, linear/radial gradient with explicit stops, stroke, optional secondary detail paths, opacity, and supported blend/interpolation. A gradient may use scene-normalized start/end coordinates or a pair of per-path landmark names (for example, root and tip). Landmark-anchored gradients resolve against each evaluated path independently, so repeated or rotated geometry receives a material gradient aligned to its own construction direction. Scene-normalized endpoints remain required fallback values and preserve compatibility with existing definitions.

Skia rendering converts the renderer-neutral style into backend paints and shaders. Silhouette is a generic render pass over geometry. Detail omission filters generic detail parts/roles, not species-specific methods. Do not build a universal shading language.

## 7. Representative rose fragment

This is a **conceptual format sketch**, not a valid shipped model or a schema-valid instance. It demonstrates which facts belong in data, but it predates the exact root/operation shape now represented by the initial v1 schema and typed DTOs. Use the schema and `ModelDefinitionJsonTests` as the current structural reference. The complete rose must additionally describe frame-derived dimensions, stem controls, two leaf attachments/profiles, veins/folds, material gradients, and final output ordering.

\`\`\`json
{
  "format": "formloom.model",
  "schemaVersion": 1,
  "id": "flowers.rose",
  "name": "Rose",
  "category": "flower",
  "prompt": {
    "article": "a",
    "noun": "rose",
    "aliases": [],
    "descriptors": { "color": ["red", "pink"] }
  },
  "parameters": {
    "variation": { "type": "number", "minimum": 0, "maximum": 1, "default": 0.35 }
  },
  "profiles": {
    "petal.broad": {
      "closed": true,
      "interpolation": "closed-cubic",
      "points": [
        [0.50, 0.02], [0.15, 0.14], [0.03, 0.39], [0.08, 0.66],
        [0.27, 0.89], [0.49, 1.00], [0.72, 0.95], [0.91, 0.76],
        [0.98, 0.48], [0.89, 0.22]
      ]
    },
    "petal.curled": {
      "closed": true,
      "interpolation": "closed-cubic",
      "points": [
        [0.50, 0.02], [0.17, 0.15], [0.05, 0.43], [0.21, 0.71],
        [0.43, 0.91], [0.65, 0.94], [0.80, 0.79], [0.65, 0.66],
        [0.43, 0.61], [0.31, 0.47], [0.38, 0.33], [0.64, 0.27],
        [0.77, 0.17]
      ]
    }
  },
  "materials": {
    "variants": {
      "red": {
        "tokens": {
          "petal.base": "#B02232",
          "petal.light": "#ED5D67",
          "petal.shadow": "#5B111F",
          "petal.edge": "#671526",
          "petal.fold": "#FF9B99",
          "stem.base": "#627C32"
        }
      },
      "pink": {
        "tokens": {
          "petal.base": "#D03A74",
          "petal.light": "#FF91BE",
          "petal.shadow": "#6F1C45",
          "petal.edge": "#842750",
          "petal.fold": "#FFC4DA",
          "stem.base": "#627C32"
        }
      }
    }
  },
  "nodes": [
    {
      "id": "bloom.outer",
      "op": "array.radial",
      "profile": "petal.broad",
      "count": 5,
      "phase": 0,
      "angleJitter": 0.055,
      "length": 0.94,
      "width": 1.04,
      "rootOffset": 0.015,
      "bend": 0.025,
      "stream": "petals.outer"
    },
    {
      "id": "bloom.middle",
      "op": "array.radial",
      "profile": "petal.broad",
      "count": 5,
      "phase": 0.5,
      "angleJitter": 0.055,
      "length": 0.72,
      "width": 0.94,
      "rootOffset": 0.035,
      "bend": 0.055,
      "stream": "petals.middle"
    },
    {
      "id": "bloom.inner",
      "op": "array.radial",
      "profile": "petal.curled",
      "count": 3,
      "phase": 0.18,
      "angleJitter": 0.055,
      "length": 0.54,
      "width": 0.78,
      "rootOffset": 0.01,
      "bend": 0.12,
      "stream": "petals.inner"
    }
  ],
  "outputs": ["stem", "leaves", "bloom.outer", "bloom.middle", "bloom.inner"]
}
\`\`\`

The example intentionally shows the fixed layer counts, phases, profile choices, scales, offsets, and palettes in data. It is not sufficient for rendering yet and must not be placed in the runtime model directory before it validates and evaluates.

## 8. Evaluated execution contract (initial implementation)

The evaluator entry point is `ModelDefinitionEvaluator.Evaluate(definition, context, options)`. The definition is validated before evaluation. The typed context supplies:

- a seed and a variation amount in 0..1;
- optional numeric parameter overrides (defaults and declared ranges are resolved by Core);
- optional choice/material-variant overrides;
- named mount frames, keyed by the mount ID declared by the definition.

The initial evaluator resolves the schema-v1 numeric expression types (literal, reference, arithmetic, clamp), including numeric parameters, mount-frame metrics, and prior-node landmarks. Numeric parameter defaults may depend on other numeric parameters; evaluation detects cycles and enforces the configured expression depth/node budgets. Overrides must target declared parameters of the correct type and remain within declared ranges.

The five current operation nodes produce the following results:

| Node | Evaluation |
|---|---|
| `path.cubic` | Explicit cubic segments and an open geometry path. |
| `curve.project` | A nearest point and tangent landmark on an earlier cubic path from `path.cubic` or `profile.map`. |
| `profile.map` | A profile mapped into a directed/scaled basis and converted to cubic path geometry. |
| `array.radial` | Deterministically varied profile instances, each with a stable path ID and optional material tone. |
| `geometry.group` | Child paths flattened into the declared group-member order. |

The final model outputs are flattened in output declaration order and passed through `ModelGeometryValidator`. The evaluator preserves path material-slot keys, optional 0..1 material tone, and named `attachment`/`root`/`tip` landmarks for profile maps. A renderer interprets path tone generically as a value hierarchy: 0 is darkest, 0.5 is neutral, and 1 is brightest; it modulates the selected solid color or each stop of the selected gradient without changing the model's palette tokens. The radial `tone` field is optional in schema v1; when absent, its base is 0 so existing v1 files remain valid.

Expected definition/context/operation failures are returned as structured diagnostics carrying the model ID and a location such as `nodes[3](bloom.outer).radiusX`. The evaluator is callable from Core tests and has no Skia or desktop dependency.

This is an initial executable graph, not a claim of complete rose construction. It does not yet reproduce the full rose's frame-derived formulas, stem/leaf/petal detail paths, material gradients, or exact reviewed rendering.

## 9. Decisions to resolve in a runnable prototype

1. Flat ordered nodes versus nested child operations for radial repetition.
2. Exact typed input unions/reference syntax for scalar, vector, path, profile, and geometry values.
3. Whether one \`profile.map\` replaces both rose mapping formulas or a second generic mode is needed.
4. How \`curve.project\` returns a point/tangent and accepts a frame-relative target.
5. How geometry exposes stable named landmarks and generic detail roles.
6. Which material interpolation preserves the rose's tone-dependent gradients and detail rendering.
7. The smallest generic vase-mount contract for opening/growth frames, clipping, and optional interior stalk continuation.
8. Whether external profiles/materials should be shared files. Prefer self-contained definitions until actual duplication justifies asset references.
9. Runtime model-root precedence and reload behavior.

Settle these through a runnable rose prototype and focused tests, not by turning the format into a speculative universal language.

## 10. Rejection criteria

Reject a format or implementation that calls a rose helper behind a generic-looking operation, requires a C# registry entry or model-ID branch, leaves counts/templates/assembly/palettes in production code, uses arbitrary scripts or network references, turns every value into an untyped dictionary, changes unrelated output when parts are added, couples Core to SkiaSharp, or requires a general scene-graph rewrite before another vase-mounted model can be added.

## 11. Next implementation sequence

1. The initial versioned schema and typed Core subset are merged in PRs [#58](https://github.com/Vraxo/Formloom/pull/58) and [#59](https://github.com/Vraxo/Formloom/pull/59); they remain a structural subset, not a complete rose definition.
2. Runtime filesystem discovery and semantic validation are merged in [PR #60](https://github.com/Vraxo/Formloom/pull/60).
3. Renderer-neutral evaluated geometry is merged in [PR #61](https://github.com/Vraxo/Formloom/pull/61).
4. The first reusable C# geometry algorithms are merged in [PR #62](https://github.com/Vraxo/Formloom/pull/62) and documented in [geometry-operations.md](geometry-operations.md). They are ordinary C# capabilities, not a declarative programming language.
5. The generic schema-v1 evaluator is implemented in [PR #63](https://github.com/Vraxo/Formloom/pull/63). It executes cubic paths, curve projection, profile mapping, radial arrays, ordered groups, bounded expressions, mount-frame references, and generic path landmarks. Linux / Release CI [run #287](https://github.com/Vraxo/Formloom/actions/runs/42781659921) passed 844/844 tests (739 Core, 105 Desktop).
6. Next: complete the rose-specific knowledge-versus-algorithm inventory; verify the C# utilities against actual rose contours and attachments; determine remaining generic material/style and geometry capabilities; then build a complete executable rose prototype. Preserve the legacy rose generator and renderer until automated and visual equivalence is reviewed.

Do not ship the draft fragment as a runtime model until it validates against the committed schema and is evaluated by the real interpreter.
