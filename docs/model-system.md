# Model system architecture contract

## Status

**Phase:** 11 — Data-driven model system  
**Contract version:** 0.1  
**Implementation status:** Architecture contract, versioned typed definitions, filesystem catalog, semantic validation, renderer-neutral evaluated geometry, reusable C# geometry algorithms, and the generic schema-v1 evaluator are implemented. Evaluated geometry carries the selected material-token palette and optional data-authored linear gradients. The standalone Skia renderer supports solid fills, linear-gradient fills, and authored strokes on model paths. The rose fixture exercises open-path strokes and palette-specific petal gradients. Richer material effects and scene integration remain unimplemented.

This document defines the intended boundary between Formloom's procedural engine and the external model knowledge it consumes. It is deliberately a contract, not a premature specification of every JSON field or procedural operator. Those details must be designed against the existing rose implementation during the next milestones.

## 1. Purpose

Formloom should become an offline, CPU-first procedural image-generation engine whose object knowledge is authored primarily in external model definitions.

Today, adding an object generally means adding or changing C# in several layers: geometry construction, prompt parsing, scene generation, scene validation, and rendering. Organizing those implementations into feature folders improved navigation, but it did not make the system data-driven.

The purpose of this milestone is to establish a different extension boundary:

> Adding a model that can be expressed using existing engine capabilities should require adding model data, not adding model-specific production C#.

A model definition is not a finished image, a set of pre-rendered sprites, or merely a bag of parameters passed to a hard-coded object generator. It is a declarative construction recipe that composes reusable procedural operations into an object.

## 2. Level 2 scope

Phase 11 targets **Level 2: data-driven procedural model construction**.

A Level 2 model definition must be able to describe, using the supported engine vocabulary:

- model identity, version, names, and prompt descriptors;
- parameters, supported appearance variants, and defaults;
- reusable shape/path templates and their control points;
- named parts and hierarchical grouping;
- construction operations and their order;
- repetition, radial arrangement, layering, and transforms;
- part anchors, attachments, and supported geometric constraints;
- material slots and data-authored material variants;
- seeded parameter ranges and controlled procedural variation.

The engine interprets those declarations and produces model-independent geometry and material references.

Level 2 is **not** a requirement to make every scene relationship data-driven. Vase containment, bowl containment, cup liquids, arbitrary multi-object composition, and general scene graphs are outside the first proof. A generic model must, however, be usable by the existing vase-content path without adding a species-specific code path. The first data-only extension will be another flower that reuses the rose-capable operator vocabulary.

## 3. Architectural responsibilities

The target responsibilities are:

| Component | Owns | Must not own |
|---|---|---|
| External model definitions | Object-specific geometry templates, part assembly, parameters, appearance variants, prompt descriptors, and variation ranges | Executable code or renderer-specific objects |
| Model catalog | Discovering definitions, resolving stable IDs, and exposing validated models | Object-specific generation algorithms |
| Definition validator | Structural, reference, operator, parameter, and constraint validation | Per-species validation branches |
| Model interpreter | Executing the finite, typed operation vocabulary and applying deterministic variation | Knowledge of particular model IDs such as `flowers.rose` |
| Generic geometry representation | Paths, curves, shapes, transforms, ordered parts, and material bindings | Species-specific Core types |
| Rendering backend | Converting generic geometry and material data into Skia drawing commands | `DrawRose`, `DrawSunflower`, or other per-model dispatch |
| Prompt/scene layer | Resolving strict prompt grammar to model IDs, parameters, and scene relationships | A new parser/generator branch for every model |

The intended flow is:

```text
Prompt
  │
  ▼
Prompt grammar + model catalog
  │
  ▼
Generic model reference + resolved parameters
  │
  ▼
Model interpreter ◄──── external model definition
  │
  ▼
Model-independent geometry and material bindings
  │
  ▼
Generic renderer
  │
  ▼
PNG
```

The Core project remains independent of SkiaSharp and desktop/UI concerns. The renderer consumes Core-level generic geometry and translates it into Skia primitives.

## 4. The model/engine boundary

### Model knowledge belongs in data

The following are examples of model knowledge and should not require a C# edit when a new model uses supported operations:

- a rose has three petal layers with counts of five, five, and three;
- a particular petal contour has a particular normalized control-point profile;
- two leaves attach at different positions along a stem;
- one layer is placed behind another;
- a model offers red and pink material variants;
- a part receives a bounded seeded variation range;
- a prompt descriptor resolves to a stable model ID.

### Reusable capabilities belong in the engine

The engine owns generic mathematical and rendering capabilities, such as:

- creating and closing paths from line and Bézier segments;
- transforming geometry;
- grouping and ordering parts;
- repeating geometry around an axis or center;
- constructing tapered strokes or curves;
- resolving named anchors and applying attachments;
- applying supported deformations;
- evaluating seeded ranges;
- rendering generic fills, strokes, gradients, clipping, and supported material effects.

This list is a **candidate vocabulary**, not a frozen operator API. Phase 11 must extract the smallest useful set from the rose implementation and test it against the second model. Avoid implementing an operator merely because it sounds generally useful.

If a new model needs a capability the engine does not have, implement a reusable operation only when its contract is model-independent. Do not add an operation named after a species or object to preserve a hard-coded generator under a different name.

### A definition is declarative, not executable

Model files must not contain arbitrary C#, scripts, dynamically evaluated expressions, or unrestricted external references. They may compose only registered, typed operations with validated parameters.

The initial implementation should prefer explicit, typed operation inputs and references over untyped dictionaries or a general expression language. Any future expression capability must be small, deterministic, safe to validate, and justified by real model requirements.

## 5. Definition format and runtime loading

JSON is the preferred initial serialization format, using the .NET `System.Text.Json` libraries already available to the application. This is a working decision for the first implementation, not permission to freeze an unreviewed schema. Phase 12 must validate the shape against the rose migration before the format is finalized.

Requirements:

- Every definition declares a schema version and stable, namespaced model ID.
- Model files are plain external files, not compiled into model-specific C# or embedded as opaque assembly resources.
- The application can discover definitions from a documented filesystem model root at runtime.
- Shipped default definitions may be distributed as ordinary files alongside the application; user-added definitions must not require rebuilding Formloom.
- Duplicate IDs, unsupported schema versions, malformed files, unknown operations, unresolved references, invalid ranges, and invalid geometry must produce actionable diagnostics.
- File loading and evaluation must remain offline. Definitions must not trigger network requests.
- File size, graph depth, operation count, repetition count, and other resource-sensitive inputs must have explicit limits.
- A definition should be independently testable in a temporary model directory, without editing a production catalog registry.

The initial catalog API uses `AppContext.BaseDirectory/models` as its default root (`LoadDefault`) and accepts an explicit custom root (`Load(rootPath)`). It recursively discovers `*.model.json` files. Each load produces a fresh immutable snapshot; calling it again reloads files added, removed, or changed since the previous call. There is no background file watcher, multi-root merge, or override precedence yet.

Catalog loading is bounded by explicit file/document/node/profile/repetition/expression limits. Malformed definitions produce path-aware diagnostics while unrelated valid definitions can still load. A duplicate model ID excludes every definition with that ID. Prompt phrases are generated from the article, material-variant descriptors, noun, and noun aliases; a phrase collision excludes every conflicting definition rather than depending on file enumeration order. The result may therefore contain usable models and diagnostics at the same time.

The default root is a runtime location, not a promise that a built-in model file ships today. Built-in definitions will be ordinary files copied beside the application when the first complete executable model is added. Until then, catalog tests and callers can supply an external directory explicitly.

## 6. Geometry and material contracts

The interpreter must produce a model-independent representation. The representation may contain typed path/curve/shape records, ordered part records, transforms, bounds, and material-slot references. It must not return a `RosePlant`, `DaisyPlant`, or other species-specific structure.

Geometry uses a documented normalized coordinate system before instance placement. Local, parent-relative, and scene-relative coordinates must be distinguishable; units and transform order must be unambiguous. Non-finite values, degenerate geometry, invalid bounds, and unsupported operations must be rejected before rendering.

Model definitions own material choices and model-specific palette values. Evaluated geometry carries the selected variant's color-token dictionary, keyed by material slot and represented as renderer-neutral hexadecimal strings. A material variant may also define linear gradients keyed by material slot; each gradient has distinct start/end points in scene-normalized coordinates and ordered stops that reference color tokens in that same variant. Evaluated geometry carries these renderer-neutral gradient records without Skia types. The Core representation must not contain `SKColor` or other SkiaSharp types. The renderer owns the conversion from generic material data to backend paint/shader resources. The initial Skia model-path renderer resolves solid tokens for strokes and fallback fills, and applies the authored linear gradient to closed-path fills. Node definitions may declare a positive stroke width as a numeric expression; the evaluator carries the resolved scene-normalized width onto each path, and the renderer uses it for stroked drawing (including open paths). The Core contract remains renderer-neutral. Clipping and richer effects remain separate capabilities. If an effect cannot be expressed through generic material/render operations, that is an engine capability gap to address deliberately—not a reason to retain a per-model renderer method.

## 7. Determinism and variation

A model evaluated with the same definition, parameters, seed, and variation input must produce the same geometry.

Random streams must be named and scoped by stable model/part/operation identity. Adding an unrelated part or changing a different layer must not casually perturb all existing parts by consuming values from one shared sequential random stream.

Definitions must express supported variation as bounded ranges or supported operations. The interpreter owns the random algorithm and stream derivation. Model files cannot select arbitrary code or random providers.

Migration must preserve the existing named-stream isolation expectations where practical. Any intentional change to the rose's output or random-stream behavior must be documented and covered by updated visual evidence, not silently accepted.

## 8. Prompt and scene integration

Formloom's current prompt parser is intentionally strict. Phase 11 does not require natural-language understanding, fuzzy matching, or arbitrary prompt composition.

The target is to keep grammar structure in the prompt/scene layer while moving model identity, descriptors, aliases, and supported appearance choices into the model catalog. A new model using an already-supported scene relationship must be resolvable without adding a species-specific parser or generator branch.

The first integration target is a single model mounted in the existing vase relationship. A general scene graph and data-driven composition rules are future work. Existing fruit, ceramic, bowl, and cup paths may remain as legacy typed implementations while the model system is proven; they must not constrain the generic model representation or require special treatment for the rose and second proof model.

## 9. Migration strategy

Use the existing rose as the first migration target because it exercises enough distinct construction techniques to test the boundary:

- broad and curled petal templates;
- three layered radial arrangements;
- one continuous curved stem;
- two attached serrated leaves and veins;
- vase-relative sizing and placement;
- material variants;
- stable, separately named random streams;
- validation of counts, attachments, proportions, and safe bounds;
- layered Skia rendering, detail passes, and silhouette rendering.

Keep the current rose implementation as the behavioral reference until the generic pipeline is verified. Do not delete the legacy path before equivalent tests and reviewed rendered output exist.

After the rose is data-driven, add a second flower (initially a sunflower) using only existing operations. This is the decisive Level 2 proof: the new model is discovered from files, resolves through the prompt vocabulary, evaluates, validates, and renders without a production C# change for that model.

## 10. Acceptance criteria

Level 2 is achieved only when all of the following are true:

1. A versioned, documented external model format exists.
2. Model definitions are discovered at runtime from the filesystem.
3. Definitions are validated with actionable file/element diagnostics.
4. A typed interpreter evaluates definitions into generic geometry.
5. The renderer handles that geometry without per-model rendering branches.
6. The rose has been migrated to an external definition and no longer depends on a rose-specific production geometry generator, scene validator, prompt/generation branch, or renderer method.
7. A second model can be added using only external model data and existing engine operations. No production C# source edit or application rebuild is needed to add it.
8. Same definition/parameters/seed/variation produce deterministic results.
9. Existing rose prompt variants and the reviewed visual behavior are preserved or any approved intentional change has explicit evidence.
10. Automated tests, full-solution Release build, Linux CI, and visual review pass.

A JSON file that only supplies parameters to `RoseGeometry.Create()` does not satisfy these criteria. Nor does a generic renderer that still switches on model IDs.

## 11. Hybrid architecture guardrail

The authoritative decision is [ADR-0001: Keep Formloom a hybrid procedural engine](adr/0001-hybrid-procedural-engine.md). This is a permanent design constraint for Phase 11 and future model-system work.

The distinction is intentional:

| Concern | Primary owner |
|---|---|
| Reusable geometry algorithms, curve mathematics, transforms, repetition, seeded random generation, and material evaluation | C# engine |
| Model profiles, proportions, counts, part composition/order, attachment choices, parameters, and appearance variants | External model data |
| Loading, validation, authoring, and persistence | The chosen format/tooling (initially JSON) |

JSON is a declarative model-description format, not a programming language. Do not translate a useful C# algorithm into a large graph of low-level JSON instructions just to claim that the model is data-driven. Add a shared C# capability only when it has a model-independent contract and solves a demonstrated requirement.

A model that fits the existing engine vocabulary should be data-authored. A genuinely specialized model may retain a code-authored algorithm when forcing it into the data format would be less clear or more fragile. This is an explicit hybrid design choice, not a failure of the model system. Do not hide a species-specific method behind a generic-sounding operation. Keep legacy paths until automated and visual equivalence is established.

Use the review questions in ADR-0001 before expanding the operation vocabulary or moving code into data. The initial C# algorithm inventory is documented in [geometry-operations.md](geometry-operations.md); it is a capability reference, not another model-definition language.

## 12. Explicit non-goals for this phase

- A universal procedural modeling language or unrestricted expression evaluator.
- User-authored C# plugins or runtime code loading.
- A neural model, external API, cloud inference, or network dependency.
- Rewriting every existing fruit, vessel, containment, or liquid model.
- General-purpose scene composition, arbitrary relationships, or a full scene graph.
- Natural-language prompt interpretation.
- Replacing SkiaSharp or the CPU-first rendering strategy.
- Removing every conditional from the engine. Conditionals that implement generic operations are expected; conditionals whose purpose is to recognize a particular model are not.

## 12. Current implementation audit (starting baseline)

The audit below is against `main` at `fc4e587166617d72d2be9679aadc5a160c79878e` (2026-11-09).

| Concern | Current rose-specific location | Why it matters |
|---|---|---|
| Geometry/templates/variation | `src/Formloom.Core/Roses/RoseGeometry.cs` | Contains petal and leaf templates, fixed layer counts, geometric mapping, attachment sampling, and named random streams. |
| Model types | `src/Formloom.Core/Roses/*.cs` | Rose has its own plant, petal, leaf, bloom, color, and vase-content records. |
| Prompt vocabulary and grammar | `src/Formloom.Core/Scenes/PromptParser.cs` | Has a rose descriptor and a dedicated `ParseVaseRose` grammar path. |
| Generation | `src/Formloom.Core/Scenes/SceneGenerator.cs` | Has a dedicated `GenerateVaseRose` branch and typed scene assignment. |
| Scene representation | `src/Formloom.Core/Scenes/StillLifeScene.cs` | Has a separate nullable `VaseRose` property alongside separate tulip, daisy, and mixed-arrangement properties. |
| Validation | `src/Formloom.Core/Scenes/SceneValidator.cs` | Counts typed flower relationships and calls a rose-specific validator with fixed species invariants. |
| Materials | `src/Formloom.Rendering.Skia/Roses/RoseMaterial.cs` | Stores red/pink rose palettes in the rendering assembly. |
| Rendering | `src/Formloom.Rendering.Skia/Scenes/StillLifeRenderer.cs` and `src/Formloom.Rendering.Skia/Roses` | Dispatches to a rose-specific drawing method and uses rose-specific material and geometry handling. |
| Behavioral evidence | `tests/Formloom.Tests/Roses`, `tests/Formloom.Desktop.Tests/Roses`, `docs/rose-vase-handoff.md`, `.agents/todo-10.md` | Provides existing parser/generator/renderer coverage and the Phase 10 visual-review handoff. |

The repository currently has no external `models/` definition tree. The existing feature-folder organization is useful and should be retained; the model system should be introduced as a separate engine capability rather than moving species folders around again.

## 14. Phase 11 decision log

- **Accepted direction:** object construction knowledge moves to external declarative model definitions; the engine provides reusable operations and interprets them.
- **Initial format:** JSON + `System.Text.Json`, pending validation during the format-design milestone.
- **Initial proof model:** migrate the rose, then add a sunflower using only the resulting operation vocabulary.
- **Initial relationship boundary:** one generic model mounted in the existing vase relationship; general scene composition is deferred.
- **Not frozen:** exact JSON field names, schema layout, C# type names, directory precedence, and final operator IDs. These require a focused design based on the rose requirements.

- **Hybrid architecture guardrail:** accepted by [ADR-0001](adr/0001-hybrid-procedural-engine.md). C# remains the home of reusable algorithms; external data describes model-specific facts and composition. JSON is not a general-purpose programming language, and specialized code-authored models remain allowed when justified.
