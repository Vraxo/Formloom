# Phase 11 — Data-driven model system

> **Status:** In progress. PRs #57–#60 are merged, including the external model catalog and semantic validation. PR 4 (renderer-neutral evaluated geometry contract) is in progress on `phase-11/generic-geometry`, based on `main` at `d19283b8d090d3323bde8b849b1184f612bc6433`. The geometry output records, bounds, and validator are implemented in the branch; PR review and CI remain pending. No evaluator, executable rose definition, or rose migration exists yet.
>
> **Original starting point:** `main` at `fc4e587166617d72d2be9679aadc5a160c79878e` (2026-11-09). Current `main` includes PR 1 merge `5e5e3952694f5ef5732483a01e8ec8b8fd203d8b`, PR 2 merge `41168813ce5fe9254179609c11b708efed7566a4`, and PR 2b merge `b41233e6c0d44738c8bb29751e5d8961ac6a17ce`; current implementation branch: `phase-11/model-catalog`.

## Goal

Move Formloom from a code-authored procedural object library to a data-driven procedural model engine.

A developer should be able to add an object model using external declarative model files and already-supported engine operations. The model catalog discovers the definition, the interpreter constructs model-independent geometry, and the generic renderer draws it. Adding a model that fits the existing operation vocabulary must not require a new model-specific production C# class, parser branch, generator branch, validator, renderer method, or application rebuild.

This phase targets **Level 2: data-driven procedural model construction**. It does not attempt a universal scripting language or a general-purpose scene graph.

## Definition of done

- [ ] Model definitions use a documented, versioned external format and are discovered from the filesystem at runtime.
- [ ] Definitions describe model-specific geometry templates, parts, assembly, parameters, materials, prompt descriptors, constraints, and seeded variation as data.
- [ ] The engine exposes a small, typed, validated, reusable operation vocabulary.
- [ ] A typed interpreter evaluates definitions into model-independent geometry.
- [ ] The renderer consumes generic geometry/material data and has no model-ID dispatch for migrated models.
- [ ] The existing rose is migrated to an external model definition; its dedicated C# geometry construction, material palette, prompt/generation path, scene validation branch, and drawing method are removed or reduced to genuinely generic facilities.
- [ ] A second model (initially a sunflower) is added using only external definition data and already-existing operations, with no production C# source edits to add that model.
- [ ] Existing rose prompt variants, determinism expectations, and reviewed visual behavior are preserved, or intentional changes have explicit visual review evidence.
- [ ] Full-solution Release build, tests, Linux CI, deterministic checks, and visual review pass.
- [ ] The model authoring and validation workflow is documented.

A file containing only parameters for `RoseGeometry.Create()` is not a data-driven model. A renderer that switches on `flowers.rose` or `flowers.sunflower` is not a generic renderer.

## Current baseline and source audit

The source audit was performed against the starting `main` commit above.

| Concern | Current location | Finding |
|---|---|---|
| Rose construction | `src/Formloom.Core/Roses/RoseGeometry.cs` | Hard-coded broad/curled petal and serrated-leaf templates, 5/5/3 layer counts, vase-relative construction, attachments, and named random streams. |
| Rose model records | `src/Formloom.Core/Roses/` | Species-specific plant, bloom, petal, leaf, color, and vase-content types. |
| Prompt parsing | `src/Formloom.Core/Scenes/PromptParser.cs` | Dedicated rose descriptor and `ParseVaseRose` parser path. |
| Scene generation | `src/Formloom.Core/Scenes/SceneGenerator.cs` | Dedicated `GenerateVaseRose` path. |
| Scene state | `src/Formloom.Core/Scenes/StillLifeScene.cs` | Separate nullable typed properties for tulip, daisy, mixed tulip/daisy, and rose vase contents. |
| Validation | `src/Formloom.Core/Scenes/SceneValidator.cs` | Species-specific rose count, layer, attachment, proportion, and bounds validation. |
| Appearance | `src/Formloom.Rendering.Skia/Roses/RoseMaterial.cs` | Red/pink palettes authored in renderer C#. |
| Rendering | `src/Formloom.Rendering.Skia/Scenes/StillLifeRenderer.cs` and `src/Formloom.Rendering.Skia/Roses/` | Species-specific dispatch and rose drawing/material logic. |

The existing Phase 10 evidence is retained in `docs/rose-vase-handoff.md`, `.agents/todo-10.md`, and the Phase 10 review artifacts referenced there. Treat those outputs as the migration baseline; do not claim a new review has occurred until the new pipeline has produced and the relevant outputs have been inspected.

The current feature-folder structure is useful and should remain. The problem is not folder organization: it is that adding a species requires production code changes across multiple layers.

## Architecture contract

The authoritative contract is [`docs/model-system.md`](../docs/model-system.md). Read it before changing the model architecture.

Core rules:

1. **Definitions own object knowledge.** Model-specific templates, part counts, part assembly, proportions, material variants, prompt descriptors, and variation ranges belong in external data.
2. **The engine owns reusable capabilities.** It implements a finite, typed vocabulary of geometry, transformation, repetition, attachment, variation, material, and rendering operations.
3. **Definitions are declarative.** No arbitrary C#, scripts, dynamic evaluation, network references, or model-specific plugins.
4. **The interpreter is generic.** It consumes validated definitions and emits model-independent geometry and material bindings.
5. **The renderer is generic for migrated models.** It draws geometry and supported materials without branching on species/model IDs.
6. **Data is genuinely external.** Adding a definition to a discovered model directory must not require compilation or a C# registry edit.
7. **Determinism is structural.** Randomness uses stable named model/part/operation streams so unrelated additions do not reshuffle existing output.
8. **Migrate incrementally.** The rose is the first proof; do not rewrite all object categories at once.
9. **Keep the scene scope honest.** A generic vase-mounted model is required for the proof. Arbitrary scene graphs and general composition are not.
10. **Do not equate configuration with construction.** Passing JSON numbers into a species-specific generator fails the objective.

## Sequential implementation checklist

### 0. Verify the starting point and capture the existing behavior

- [x] Confirm the repository is `Vraxo/Formloom`, its default branch is `main`, and the starting main commit is `fc4e587166617d72d2be9679aadc5a160c79878e`.
- [x] Inspect the current source tree and confirm the feature-folder organization from PR #56 is present.
- [x] Inspect the rose implementation, prompt parser, generator, scene record, validator, Skia material, renderer dispatch, and existing Phase 10 handoff.
- [ ] Re-read the complete Phase 10 checklist and handoff and identify the exact canonical/robustness PNGs and timing sidecars to preserve as the comparison baseline.
- [x] Record the latest verified unmodified-main full-solution Release build/test run: Linux / Release run [42622879601](https://github.com/Vraxo/Formloom/actions/runs/42622879601) passed on starting commit `fc4e587166617d72d2be9679aadc5a160c79878e`.
- [x] Record the inherited Phase 10 visual artifacts in `docs/rose-vase-handoff.md`: focused bundle [13663464131](https://github.com/Vraxo/Formloom/actions/runs/42597119777/artifacts/13663464131) and procedural-preview [13663518844](https://github.com/Vraxo/Formloom/actions/runs/42597119777/artifacts/13663518844). These are comparison references, not a new Phase 11 visual review.
- [x] Confirm existing Phase 10 tests cover the four rose prompt/material combinations, rejected prompts, stable subject tokens, structural counts, deterministic rendering, canvas dimensions, silhouettes, and detail behavior.
- [ ] Record an explicit file-by-file baseline manifest of the canonical/robustness PNGs and timing sidecars before replacing the legacy implementation.

### 1. Architecture contract and baseline documentation

**PR 1 — contract and audit.** Keep this change documentation-only unless a concrete documentation/build issue requires otherwise.

- [x] Write `docs/model-system.md` with the Level 2 boundary, architectural responsibilities, declarative-data rule, format direction, geometry/material boundary, determinism requirements, migration strategy, non-goals, acceptance criteria, and current source audit.
- [x] Add this Phase 11 checklist with PR-sized milestones and evidence-based acceptance gates.
- [x] Review the contract against the actual rose implementation and ensure it does not assume operations the current engine has not demonstrated. Candidate operators are explicitly provisional.
- [x] Verify links and paths in `docs/model-system.md` and `.agents/todo-11.md`.
- [x] Open PR [#57](https://github.com/Vraxo/Formloom/pull/57) against `main` as a documentation-only change.
- [x] Obtain passing PR Linux / Release CI run [42628176874](https://github.com/Vraxo/Formloom/actions/runs/42628176874) on `77cbdc79d550304e7d3176eee051edbe67744c48`.
- [x] Merge PR #57 as `5e5e3952694f5ef5732483a01e8ec8b8fd203d8b` and verify post-merge Linux / Release CI run [42628463314](https://github.com/Vraxo/Formloom/actions/runs/42628463314) passed.
- [x] Record that the merge completes documentation only; it does not complete any runtime model-system milestone.

**Acceptance gate:** Complete. The repository now has a shared Level 2 contract and a source audit. No schema or operator API was frozen by PR #57.

### 2. Model format and typed definition design

**PR 2 — versioned definition contract, schema, and typed representation.** The design draft is in [`docs/model-definition-format.md`](model-definition-format.md). It is explicitly provisional until exercised by a runnable rose prototype.

- [x] Re-read `docs/model-system.md` and enumerate the exact data required to represent the existing rose without invoking rose-specific code.
- [x] Select JSON with `System.Text.Json` as the initial external serialization direction; keep exact fields provisional until the model fixture and typed operation contracts are tested.
- [x] Propose stable, namespaced model IDs (for example, `flowers.rose`) and an integer schema version. Final validation rules remain open.
- [x] Draft the coordinate spaces, generic `vase-opening` attachment frame, profile mapping, candidate operation responsibilities, deterministic repeated-element streams, material roles, and constrained numeric-expression boundary in `docs/model-definition-format.md`.
- [ ] Define typed C# representation records for metadata, prompt descriptors, parameters, variants, parts, operation nodes, anchors, constraints, material slots, and variation ranges as actually required.
- [ ] Finalize the normalized coordinate system, coordinate spaces, units, transform order, bounds semantics, and parent/local reference rules from the draft after exercising the rose fixture.
- [ ] Define how a model declares material data without referencing SkiaSharp types.
- [ ] Define how definitions refer to other reusable templates/definitions, if the rose requires such reuse; reject unresolved and cyclic references.
- [ ] Define explicit resource limits for document size, node/depth counts, arrays, repetition counts, and numeric ranges.
- [x] Include a representative rose construction fragment in `docs/model-definition-format.md`; it is clearly labeled as a sketch, not a valid runtime model.
- [ ] Convert the draft into a JSON Schema and a complete, schema-valid rose fixture.
- [ ] Add format-level tests for valid documents, unsupported schema versions, missing/duplicate identifiers, malformed values, unknown fields/operators where relevant, and invalid references.
- [x] Merge the provisional format design as PR [#58](https://github.com/Vraxo/Formloom/pull/58), commit `41168813ce5fe9254179609c11b708efed7566a4`; PR Linux / Release CI [42629742765](https://github.com/Vraxo/Formloom/actions/runs/42629742765) and post-merge CI [42629995928](https://github.com/Vraxo/Formloom/actions/runs/42629995928) both passed.

**PR 2b — machine-checkable schema and typed Core representation.** Start the code contract without implementing file discovery or model evaluation:

- [x] Add `schemas/model-definition.v1.schema.json` using JSON Schema 2020-12 for the initial typed subset.
- [x] Add renderer-neutral typed Core records for model metadata, prompt descriptors, parameters, profiles, material variants, points, numeric expressions, and the five initial operation variants.
- [x] Configure `System.Text.Json` polymorphic deserialization with explicit operation discriminators; unknown operation IDs fail rather than silently fall back.
- [x] Keep all DTOs in Core and renderer-neutral, with no Skia types, file I/O, or object-specific C# classes.
- [x] Add focused round-trip/deserialization tests for each supported union member and representative model roots.
- [ ] Add a complete rose fixture only after every required construction concept has a typed representation; do not bless the current incomplete fragment as executable.
- [x] Confirm the initial schema and serializer agree on required fields, enums, discriminators, and unknown-property behavior for the covered subset.

**Acceptance gate:** A schema-valid definition can deserialize into a typed, renderer-neutral Core object graph; unsupported operations and malformed structure are rejected. This gate does not claim runtime discovery or geometry evaluation.
- [x] Explicitly reject a general-purpose expression language, embedded scripts, and arbitrary plugins. The format draft allows only a small typed numeric expression tree if the vase-frame formulas prove it necessary; that limited proposal must be validated or removed during prototyping.

**Acceptance gate:** The versioned format can describe the rose's static knowledge and intended construction declaratively. Its C# representation is typed; it is not a bag of untyped JSON values.

### 3. Runtime model catalog, loading, and validation

**PR 3 — external discovery and diagnostics.**

- [x] Add a model catalog that discovers definitions from an explicit filesystem root at runtime.
- [x] Support a documented default model directory (`AppContext.BaseDirectory/models`) and a user-supplied/custom model directory through the explicit-root API.
- [ ] Distribute complete built-in model files as ordinary files alongside the application; no complete executable model definition exists yet.
- [x] Make duplicate IDs, duplicate normalized prompt phrases, invalid schema versions, missing roots/files, and malformed definitions observable as structured diagnostics; all conflicting definitions are excluded.
- [x] Validate typed references, supported operation types/parameters, finite numeric literals/profile points, choice/range structure, ordered graph dependencies, graph depth, anchors exposed by the initial mount contract, and resource limits before evaluation.
- [x] Use the repository's Result/error-value conventions for expected file/definition errors; expected failures return structured diagnostics.
- [x] Ensure the catalog can load a temporary test directory so tests prove file-based discovery independently of the production model set.
- [x] Test missing, malformed, invalid, duplicate, and valid model directories, including discovering a newly added definition on a subsequent load. Cover normalized cross-model prompt collisions, duplicate aliases, forward references, graph-depth and total-repeat limits.
- [x] Document filesystem discovery, default/custom roots, diagnostic behavior, and reload semantics.

**Acceptance gate:** Complete. A valid external definition is discoverable and malformed or semantically invalid definitions produce useful diagnostics before geometry evaluation. The catalog is filesystem-backed and takes a fresh immutable snapshot on each load.

**PR 3 verification:** PR [#60](https://github.com/Vraxo/Formloom/pull/60), head `6d1b799fc9b94c22efcae3d99a341071a35d4dc1`; Linux / Release CI [run #269](https://github.com/Vraxo/Formloom/actions/runs/42641157403) passed. TRX artifacts report 708/708 Core tests and 105/105 Desktop tests (813 total). Workflow artifacts: test results [13685681343](https://github.com/Vraxo/Formloom/actions/runs/42641157403/artifacts/13685681343), procedural preview [13685995955](https://github.com/Vraxo/Formloom/actions/runs/42641157403/artifacts/13685995955), and inherited Phase 10 rose review bundle [13685691345](https://github.com/Vraxo/Formloom/actions/runs/42641157403/artifacts/13685691345). This PR does not change geometry or rendering; the rose bundle remains a legacy comparison baseline, not evidence of a migrated rose.

### 4. Generic geometry representation

**PR 4 — model-independent evaluated geometry.**

- [ ] Design a typed Core representation for the evaluator's output: normalized paths/curves/shapes, hierarchy or flattened ordered parts, transforms, bounds, and material bindings.
- [ ] Represent fill, stroke, closure, path segments, and draw ordering in a renderer-neutral form.
- [ ] Keep SkiaSharp, `SKPath`, `SKColor`, and other backend types out of Core and the model definition types.
- [ ] Represent part identity generically so deterministic streams and diagnostics can refer to stable model/part/operator IDs.
- [ ] Define whether the evaluator emits an immutable geometry document or a typed ordered operation result; document the choice and lifetime/ownership rules.
- [ ] Add geometry validation for non-finite values, degenerate paths, unsupported transforms, invalid bounds, and invalid material references.
- [ ] Test coordinate-space mapping, transform composition, path closure, bounds, and deterministic serialization/equality as appropriate.
- [ ] Avoid species-specific records such as `RoseGeometryDocument` or `SunflowerGeometryDocument`.

**Acceptance gate:** Generic geometry can represent all structural output required by the rose without containing rose-specific types or Skia types.

### 4. Generic geometry representation

**PR 4 — renderer-neutral output representation.** This milestone defines the stable output contract between a future model interpreter and rendering backends. It does not evaluate model-definition nodes or change the existing Skia renderer.

- [x] Define typed geometry-document and path records in Core, independent of SkiaSharp and species-specific types.
- [x] Define the evaluated coordinate-space contract. For this first pipeline, all emitted path coordinates are scene-normalized (positive X right, positive Y down); local/profile coordinates must be resolved by the evaluator before emission.
- [x] Preserve explicit path identity, source-node identity, ordered draw sequence, open/closed semantics, material-slot binding, and detail-pass metadata.
- [x] Define conservative path bounds from cubic control hulls so validation and later render planning can safely reason about extents.
- [x] Validate required IDs, unique path IDs, supported coordinate space, non-empty paths/segments, finite coordinates, segment continuity, and closed-path closure with structured diagnostics.
- [x] Test multiple paths and their order, open/closed paths, invalid values/references, disconnected segments, and conservative bounds.
- [x] Document why the output is flattened into an ordered path list: the definition graph retains grouping, while the first renderer contract needs explicit stable draw order rather than renderer-specific group objects.
- [x] Verify all new geometry types remain in Core and reference only Core geometry primitives (CubicSegment and Vector2), with no Skia or scene/species types.
- [ ] Open PR 4 against main; pass full-solution Release build and Linux CI before marking this milestone complete.

**Acceptance gate:** A renderer can receive one validated ModelGeometry value and draw its ordered paths without knowing the model ID or reading the source definition. This gate establishes the data contract only; no evaluator or renderer integration is claimed.

### 5. Extract and implement the minimum reusable operation vocabulary

**PR 5 — generic operators, driven by the rose requirements.**

- [ ] Decompose `RoseGeometry.Create` into model knowledge versus reusable mathematics before implementing operators.
- [ ] Identify the smallest operation set that can express the rose: likely path/template, transform, group, ordered layer, radial repetition, curve/taper, attachment/anchor, bounded seeded variation, and material binding.
- [ ] Treat the list as provisional; add an operation only when a rose requirement or the data-only proof model needs it.
- [ ] Give every operation a stable ID, typed inputs, explicit coordinate behavior, output contract, validation rules, and deterministic random-stream policy.
- [ ] Implement operations as reusable engine capabilities, not wrappers around `RoseGeometry` or other species methods.
- [ ] Make repeated elements stable under unrelated part additions by deriving streams from model/part/operation identity and element index.
- [ ] Add focused tests for each operator, including boundary/error cases, finite output, transform/anchor semantics, and repeatable seeded behavior.
- [ ] Verify the same operators can be composed in more than one model fixture where possible.
- [ ] Do not add `rose-petal`, `rose-leaf`, `create-sunflower`, or other species-named operations.

**Acceptance gate:** The operation vocabulary is small, typed, reusable, deterministic, and sufficient to construct the current rose without a rose-specific generator.

### 6. Model interpreter/evaluator

**PR 6 — execute validated definitions.**

- [ ] Implement a generic evaluator that accepts a validated definition, resolved parameters/variants, a seed, and variation amount.
- [ ] Evaluate declared parts and operations into the generic geometry representation.
- [ ] Resolve named anchors and attachments with explicit error handling for missing or invalid targets.
- [ ] Apply material bindings and bounded parameter variation.
- [ ] Keep evaluation deterministic for the same definition, parameters, seed, variation, and evaluator/schema version.
- [ ] Ensure the evaluator does not branch on model IDs, species names, prompt strings, or material variant names.
- [ ] Make the evaluator callable from Core tests without the desktop application or Skia renderer.
- [ ] Add tests for valid composition, missing references, cycles, invalid operation inputs, bounds, seed stability, named-stream isolation, and variation extremes.
- [ ] Include diagnostic context (model ID, part ID, operation ID, and definition path) in evaluation failures.

**Acceptance gate:** A test can load a definition, evaluate it with a seed, and inspect generic geometry without calling a model-specific class.

### 7. Migrate rose construction and materials to external data

**PR 7 — rose as the first data-driven model.**

- [ ] Create the external rose definition under the selected model root.
- [ ] Move petal control-point templates, leaf outline template, fixed layer counts, phases, layer scales, root offsets, curl/tone settings, dimensions, attachment data, and supported variation ranges into model data.
- [ ] Express the rose's one continuous curved stem, exactly two serrated leaves, and 5/5/3 petal layers through reusable operators.
- [ ] Express red and pink palette variants through renderer-neutral model material data.
- [ ] Preserve vase-profile-relative sizing, headroom, mouth attachment, safe bounds, and existing model proportions through generic anchors/parameters/constraints.
- [ ] Preserve stable rose random-stream behavior where possible and document any intentional output differences.
- [ ] Make the rose model ID and prompt descriptors discoverable from the catalog rather than from a C# registration list.
- [ ] During migration, keep the legacy geometry path available only as a comparison/reference until generic output is validated.
- [ ] Compare canonical rose output, both material variants, both vase glazes, robustness seeds, variation extremes, silhouettes, details, and supported canvas sizes against the Phase 10 baseline.
- [ ] Remove the production rose construction algorithm only after generic-path tests and visual review pass.

**Acceptance gate:** The rose is produced by evaluating an external definition; no rose-specific construction code is required by the new path.

### 8. Generic vase-model, prompt, scene, validation, and renderer integration

**PR 8 — connect generic models to the application.**

- [ ] Introduce a generic procedural model instance/reference in the scene representation for the migrated model path.
- [ ] Replace the separate rose-only vase-content relationship with a generic model-in-vase attachment usable by another model without a new scene property.
- [ ] Make the strict vase prompt grammar resolve model identity and appearance descriptors through catalog data while retaining whole-prompt rejection for unsupported grammar.
- [ ] Ensure prompt resolution and generation are generic for every definition using the supported vase relationship.
- [ ] Replace rose-specific scene-validation logic with generic model-instance, geometry, anchor, bounds, and vase-opening checks.
- [ ] Render generic geometry through shared path/curve/material/layer operations. Keep vase clipping/occlusion and near-rim continuity correct without a rose drawing method.
- [ ] Preserve silhouette, detail-omission, diagnostic, antialiasing, shadow-mask, and supported-resolution behavior.
- [ ] Ensure default PNG subject tokens/save names are derived safely from generic model/variant metadata or a generic naming policy, not hard-coded rose branches.
- [ ] Preserve the four existing rose prompts and their expected subject tokens.
- [ ] Keep old tulip/daisy/pair paths working during the transition; do not broaden this PR into a rewrite of every current scene category.
- [ ] Add end-to-end Core, rendering, and Desktop tests for the generic model route.

**Acceptance gate:** All current rose prompts run through catalog → generic instance → evaluator → generic renderer, without a rose-specific prompt/generator/validator/render branch.

### 9. Data-only extension proof with a second model

**PR 9 — sunflower (or an equally discriminating second flower).**

- [ ] Implement a sunflower definition using only operations already present before the model is added.
- [ ] Define its disk, radial petals, stem, leaves, material variants, parameters, and prompt descriptors entirely in external model data.
- [ ] Add the definition to a temporary or external model directory and verify catalog discovery without editing a C# registration list.
- [ ] Generate it through the same generic vase-content relationship and renderer as the rose.
- [ ] Verify prompt recognition, material selection, geometry bounds, attachment/occlusion, determinism, variation, silhouettes, and output dimensions.
- [ ] Prove that adding the definition requires no production C# source changes and no application rebuild. Test this with an automated fixture/catalog test and record the production-source diff boundary in the handoff.
- [ ] If the sunflower exposes a missing operation, add only a model-independent operation with focused tests, then re-run the data-only proof after that operator exists. The sunflower definition itself must not introduce a special-case code path.
- [ ] Produce canonical/robustness visual-review artifacts and inspect them before accepting the proof.

**Acceptance gate:** A second distinct model is loaded, recognized, constructed, and rendered from external data alone using the already-implemented engine vocabulary.

### 10. Cleanup, authoring workflow, and final verification

**PR 10 — complete Level 2 and close the phase.**

- [ ] Audit migrated paths for model-ID/species-name conditionals and remove remaining per-model branches.
- [ ] Remove superseded rose Core geometry/types, Skia material/drawing code, parser/generator methods, and rose-specific validation only after all required tests and visual baselines are migrated.
- [ ] Confirm the rose and second model are not embedded as code defaults or manually registered.
- [ ] Document the model authoring workflow from a new directory/file through validation, prompt descriptors, preview, and rendered output.
- [ ] Provide a validation/preview command or equivalent developer workflow that can inspect definitions without launching the full desktop UI, if it fits the existing app architecture without a disproportionate CLI subsystem.
- [ ] Document built-in versus user model locations, schema compatibility, operator reference, coordinate conventions, material fields, seeded variation, and diagnostics.
- [ ] Update README capability descriptions to distinguish model definitions from legacy code-authored capabilities still outside the migrated scope.
- [ ] Run full-solution Release build with warnings as errors and the entire test suite.
- [ ] Run Linux / Release CI and inspect its test results and generated PNG artifacts.
- [ ] Inspect canonical and robustness images at review size and full resolution; inspect silhouettes and material/layer details.
- [ ] Record timing as observations with environment and scope, never as universal performance guarantees.
- [ ] Write a Phase 11 handoff with exact merged PRs, commits, CI runs, artifact IDs, reviewed images, remaining limitations, and the next recommended phase.
- [ ] Reconcile this checklist with the merged source and evidence. Do not check off tasks based only on an agent's claim.

**Final acceptance gate:** A contributor can add a new model to the documented external model directory using existing operations, restart or reload the application according to the documented lifecycle, use its prompt descriptor, and obtain a deterministic rendered result without modifying or recompiling production C#.

## PR and handoff ledger

| Unit | Scope | Status | Evidence |
|---|---|---|---|
| PR 1 | Architecture contract and source audit | Complete | [PR #57](https://github.com/Vraxo/Formloom/pull/57), merge `5e5e395`, PR CI `42628176874`, main CI `42628463314` |
| PR 2 | Model-definition format design draft | Complete | [PR #58](https://github.com/Vraxo/Formloom/pull/58), merge `4116881`, PR CI `42629742765`, main CI `42629995928` |
| PR 2b | Initial JSON Schema and typed Core definition records | Complete (initial structural subset) | [PR #59](https://github.com/Vraxo/Formloom/pull/59), merge `b41233e`, PR CI [42631769203](https://github.com/Vraxo/Formloom/actions/runs/42631769203), main CI [42632077052](https://github.com/Vraxo/Formloom/actions/runs/42632077052) |
| PR 3 | Runtime model catalog and semantic file validation | PR CI passed; merge/main CI pending | [PR #60](https://github.com/Vraxo/Formloom/pull/60), head `6d1b799`, Linux / Release run [42641157403](https://github.com/Vraxo/Formloom/actions/runs/42641157403), 813/813 tests |
| PR 4 | Generic geometry representation | Not started | — |
| PR 5 | Reusable procedural operators | Not started | — |
| PR 6 | Generic model interpreter | Not started | — |
| PR 7 | Rose definition migration | Not started | — |
| PR 8 | Generic scene/prompt/renderer integration | Not started | — |
| PR 9 | Data-only second model proof | Not started | — |
| PR 10 | Cleanup, documentation, final verification | Not started | — |

## Agent execution and handoff rules

- Work from the latest `main` and record the exact starting commit for each PR-sized unit.
- Inspect the current repository before editing; never assume paths or APIs from an earlier phase still exist.
- Keep PRs small enough that schema/operator/renderer decisions can be reviewed independently.
- Follow repository style: one class per file, no tabs, small helpers, no speculative abstractions, and explicit Result/error values for expected failures.
- Do not introduce a package dependency without a demonstrated need; JSON support should use the .NET platform libraries initially.
- Keep model definitions and generic model fixtures in ordinary files so tests can exercise external discovery.
- Update this checklist as work proceeds. Check an item only after the implementation or evidence is inspected.
- Run focused tests during each PR and the full Release solution build/test before declaring a milestone complete.
- Preserve existing PNG baselines and create new canonical/robustness artifacts for geometry or renderer changes.
- Do not claim visual acceptance from a successful build or artifact upload; inspect the images.
- CI must pass on the PR commit and, after merge, on the resulting `main` commit before closing the corresponding handoff.
- The final handoff must include a concise status line, exact PR/commit/run/artifact identifiers, tests and visual evidence, remaining limitations, and the next concrete action.

## Handoff

**Current status:** PRs #57, #58, and #59 are merged with passing PR and post-merge Linux / Release CI. PR #60 is open; its current head `6d1b799fc9b94c22efcae3d99a341071a35d4dc1` passed Linux / Release run [42641157403](https://github.com/Vraxo/Formloom/actions/runs/42641157403): Release build succeeded and all 813 tests passed (708 Core, 105 Desktop). Run artifacts: test results [13685681343](https://github.com/Vraxo/Formloom/actions/runs/42641157403/artifacts/13685681343), procedural preview [13685995955](https://github.com/Vraxo/Formloom/actions/runs/42641157403/artifacts/13685995955), and Phase 10 rose comparison bundle [13685691345](https://github.com/Vraxo/Formloom/actions/runs/42641157403/artifacts/13685691345). The catalog milestone is implemented and PR-tested; merge and post-merge `main` CI remain pending. No built-in executable model definition, generic geometry representation, interpreter, or rose migration exists yet. Do not call Level 2 complete until the data-only second-model proof passes.
