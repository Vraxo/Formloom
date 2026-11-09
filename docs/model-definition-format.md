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

Conversions are explicit operations. The renderer must never guess a point's coordinate space. A profile declares open/closed state, interpolation, and ordinary numeric point/control-point data. The rose needs closed-cubic interpolation for closed contours and explicit cubic segments for open paths.

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

The generic style vocabulary should begin with only what the rose needs: solid fill, linear/radial gradient with explicit stops, stroke, optional secondary detail paths, opacity, and supported blend/interpolation. Gradient endpoints may reference geometry landmarks such as a petal's \`root\` and \`tip\`; they must not be hard-coded scene coordinates.

Skia rendering converts the renderer-neutral style into backend paints and shaders. Silhouette is a generic render pass over geometry. Detail omission filters generic detail parts/roles, not species-specific methods. Do not build a universal shading language.

## 6. Representative rose fragment

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

## 7. Decisions to resolve in a runnable prototype

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

## 8. Rejection criteria

Reject a format or implementation that calls a rose helper behind a generic-looking operation, requires a C# registry entry or model-ID branch, leaves counts/templates/assembly/palettes in production code, uses arbitrary scripts or network references, turns every value into an untyped dictionary, changes unrelated output when parts are added, couples Core to SkiaSharp, or requires a general scene-graph rewrite before another vase-mounted model can be added.

## 9. Next implementation sequence

1. The initial versioned schema and typed Core subset are merged in PRs [#58](https://github.com/Vraxo/Formloom/pull/58) and [#59](https://github.com/Vraxo/Formloom/pull/59). They are intentionally not yet a complete executable rose contract.
2. Runtime filesystem discovery and semantic validation are merged in [PR #60](https://github.com/Vraxo/Formloom/pull/60); post-merge Linux / Release CI [run #271](https://github.com/Vraxo/Formloom/actions/runs/42646802073) passed.
3. The renderer-neutral evaluated geometry output contract is implemented in [PR #61](https://github.com/Vraxo/Formloom/pull/61). Its latest PR head `584a7bc1d890719e4496c7fde9109373a21d8d78` passed Linux / Release CI [run #273](https://github.com/Vraxo/Formloom/actions/runs/42675732224), including 821/821 tests. Merge and post-merge CI remain pending.
4. Before expanding operators, follow [ADR-0001](adr/0001-hybrid-procedural-engine.md): decompose the real rose into reusable C# algorithms versus model-specific declarative knowledge. Add only the small set of model-independent operations demonstrated by that audit. Then implement the evaluator and only afterward author the external rose definition.

Do not ship the draft fragment as a runtime model until it validates against the committed schema and is evaluated by the real interpreter.
