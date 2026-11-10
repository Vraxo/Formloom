# Phase 11 — Data-driven model system

> **Current status (2026-11-10):** The original status and starting-point notes in this checklist predate PRs #64–#77 and are historical; they do not describe current `main`. Current architecture and implementation status are maintained in [the model-system contract](../docs/model-system.md) and [rose model inventory](../docs/rose-model-inventory.md). The accepted default visual direction is documented in [art direction](../docs/art-direction.md). Current `main` is `996b54e3b771293918dabbd9a396894a4d9e6910`. The schema-v1 definitions, catalog, validator, evaluator, reusable geometry operations, and standalone generic model renderer have advanced substantially. The renderer supports solid/gradient fills, authored strokes, detail omission, and silhouettes, but it is not connected to the desktop generation path. The rose fixture remains an expressiveness probe, not a complete migrated production model. Full rose migration, complete material/style rendering, host-vessel integration, application prompt/scene integration, and the data-only second-model proof remain outstanding.
>
> **Historical starting point:** `main` at `fc4e587166617d72d2be9679aadc5a160c79878e` (2026-11-09). The original audit and checklist below were written against that baseline. Use the current architecture documents and latest merged `main` before selecting or implementing further work.

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

**Permanent guardrail:** [ADR-0001 — Keep Formloom a hybrid procedural engine](../docs/adr/0001-hybrid-procedural-engine.md) is accepted and governs this phase. Reusable algorithms belong in C#; model-specific facts and composition belong in external data. JSON is not a programming language, and a genuinely specialized model may remain code-authored when a data graph would be less clear or robust. Every operator/data-boundary decision must be reviewed against the ADR.

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

**PR 4 — renderer-neutral output representation.** This milestone defines the stable output contract between a future model interpreter and rendering backends. It does not evaluate model-definition nodes or change the existing Skia renderer.

- [x] Define typed geometry-document and path records in Core, independent of SkiaSharp and species-specific types.
- [x] Define the evaluated coordinate-space contract. For this first pipeline, all emitted path coordinates are scene-normalized (positive X right, positive Y down); local/profile coordinates must be resolved by the evaluator before emission.
- [x] Preserve explicit path identity, source-node identity, ordered draw sequence, open/closed semantics, material-slot binding, and detail-pass metadata.
- [x] Define conservative path bounds from cubic control hulls so validation and later render planning can safely reason about extents.
- [x] Validate required IDs, unique path IDs, supported coordinate space, non-empty paths/segments, finite coordinates, segment continuity, and closed-path closure with structured diagnostics.
- [x] Test multiple paths and their order, open/closed paths, invalid values/references, disconnected segments, and conservative bounds.
- [x] Document why the output is flattened into an ordered path list: the definition graph retains grouping, while the first renderer contract needs explicit stable draw order rather than renderer-specific group objects.
- [x] Verify all new geometry types remain in Core and reference only Core geometry primitives (CubicSegment and Vector2), with no Skia or scene/species types.
- [x] Merge PR 4 [#61](https://github.com/Vraxo/Formloom/pull/61) as `13fdc700b9e32c1e8e171a1b7a535d4ee0cda30d`; PR Linux / Release CI passed and post-merge main run [#282](https://github.com/Vraxo/Formloom/actions/runs/42676443120) succeeded.

**Acceptance gate:** A renderer can receive one validated ModelGeometry value and draw its ordered paths without knowing the model ID or reading the source definition. This gate establishes the data contract only; no evaluator or renderer integration is claimed.

### 5. Extract and implement the minimum reusable operation vocabulary

**PR 5 — generic C# capabilities, driven by the rose requirements.** The permanent boundary is ADR-0001: reusable algorithms live in C#; external model data describes model-specific profiles, values, and composition. Do not turn algorithms into JSON graphs for the sake of data purity.

- [x] Record the permanent hybrid-architecture decision in [ADR-0001](../docs/adr/0001-hybrid-procedural-engine.md) and reference it from the model-system contract, format draft, and this checklist.
- [x] Audit the rose construction source and identify reusable math candidates: normalized profile mapping (petal and leaf contours), radial repetition with bounded variation, and curve projection for attachments.
- [x] Add `ProfileMapper` as a model-independent C# algorithm for oriented, scaled profile mapping. Its mapping options cover the common petal/leaf mapping formula without petal- or leaf-specific code.
- [x] Add `RadialArrayGenerator` as a model-independent C# algorithm. It returns ordered element angles and variation values using stable per-element named random streams.
- [x] Add `CubicCurveProjector` as a model-independent C# algorithm for nearest-point projection onto one or more cubic segments.
- [x] Add focused tests for mapping formulas/orientation/reversal, radial spacing/phase/determinism/stream isolation, nearest curve projection/tangents, and invalid inputs.
- [x] Document operation semantics and explicitly identify which source-definition concerns remain interpreter or renderer responsibilities in `docs/geometry-operations.md`.
- [x] Review the operation boundary against ADR-0001; operations are independent of JSON DTOs and model IDs.
- [ ] Complete the full rose data-versus-algorithm inventory (including frame-derived sizing, stem construction, leaf anatomy/veins, petal folds/tone, materials, and draw order).
- [ ] Decide how validated definition nodes and evaluated expressions invoke these C# capabilities in the generic interpreter.
- [ ] Connect operation outputs to `ModelGeometry` and define generic handling for path construction and ordered grouping.
- [ ] Verify profile mapping and curve projection against actual rose contours/attachments; compare output against Phase 10 evidence before changing or removing legacy code.
- [ ] Add further shared capabilities only when the rose or second-model proof demonstrates a model-independent need; keep specialized algorithms in C# when data composition would be less clear.
- [ ] Verify reuse with a second model fixture; do not create species-named operations.

**Acceptance gate:** The shared C# operation vocabulary is sufficient to express the common rose construction mathematics without model-specific branches. This gate is not complete until the interpreter and rose prototype prove it in context.

### 6. Model interpreter/evaluator

**PR 6 — execute validated definitions.**

- [x] Implement a generic evaluator that accepts a validated definition plus a typed context of parameter overrides, appearance-choice overrides, seed, variation amount, and declared mount-frame values.
- [x] Evaluate the five current schema-v1 node types into the generic geometry representation: cubic paths, curve projection, profile mapping, radial arrays, and ordered geometry groups.
- [x] Resolve numeric literals, parameter references, mount-frame references, prior-node landmarks, arithmetic, and clamp expressions with explicit depth/node limits.
- [x] Resolve numeric parameter defaults/overrides recursively, enforce declared min/max bounds, resolve choice defaults/overrides, and detect numeric parameter dependency cycles.
- [x] Resolve named curve-projection and profile-map landmarks; missing mount metrics/references fail with structured diagnostics.
- [x] Preserve material-slot bindings and optional per-path material tone in geometry output. Radial base tone is an optional schema-v1 field (default 0) to preserve compatibility with definitions authored before it existed.
- [x] Apply bounded deterministic radial variation by calling the reusable C# operation; repeated output is stable for the same definition/context/seed.
- [x] Keep evaluator Core-only, model-ID agnostic, renderer-independent, and free of JSON serialization dependencies.
- [x] Add Core tests for multi-node composition, mounted frame values, anchor projection, material metadata, seeded repeatability, invalid choices/overrides, missing frame values, division by zero, and parameter cycles.
- [x] Include model ID and node index/ID in structured evaluation diagnostics.
- [x] Add evaluator tests for finite-coordinate range/overflow and variation extremes; retain shared operator tests for stream-isolation behavior.
- [ ] Verify every output path and landmark against the actual rose geometry templates/attachment expectations; no legacy geometry has been replaced yet.
- [ ] Decide whether future profile/path operations need additional model-independent behavior; do not expand the operator set without a concrete rose or second-model requirement.
- [x] Open PR 6 against PR 5 and pass full-solution Release build/Linux CI: PR [#63](https://github.com/Vraxo/Formloom/pull/63), head `a1d6f3a`, run [#287](https://github.com/Vraxo/Formloom/actions/runs/42781659921), 844/844 tests.

**Acceptance gate:** A test can evaluate a validated definition using its typed context and inspect generic ordered geometry without calling a model-specific class. Rose equivalence, material rendering, and scene integration remain later milestones.


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
| PR 3 | Runtime model catalog and semantic file validation | Merged | [PR #60](https://github.com/Vraxo/Formloom/pull/60), merged in main `d19283b`, post-merge Linux / Release run [42646802073](https://github.com/Vraxo/Formloom/actions/runs/42646802073) |
| PR 4 | Renderer-neutral evaluated geometry output contract | Merged | [PR #61](https://github.com/Vraxo/Formloom/pull/61), merge `13fdc700`, post-merge Linux / Release run [42676443120](https://github.com/Vraxo/Formloom/actions/runs/42676443120) passed |
| PR 5 | Reusable C# geometry algorithms | Merged | [PR #62](https://github.com/Vraxo/Formloom/pull/62), merge `895d672`, rebased Release CI [run #283](https://github.com/Vraxo/Formloom/actions/runs/42678839932), 832/832 tests (727 Core, 105 Desktop) |
| PR 6 | Generic schema-v1 model evaluator | PR CI passed; merge pending | [PR #63](https://github.com/Vraxo/Formloom/pull/63), head `a1d6f3a`, Linux / Release run [42781659921](https://github.com/Vraxo/Formloom/actions/runs/42781659921), 844/844 tests (739 Core, 105 Desktop) |
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

**Current status:** PRs #57–#62 are merged. PR #62's reusable geometry operations are on `main` at `895d672`, and rebased-baseline Linux / Release run [#283](https://github.com/Vraxo/Formloom/actions/runs/42678839932) passed 832/832 tests. PR #63's evaluator is open at `a1d6f3aeb862fcf509c5c46f488ae4e56767372e`; Linux / Release run [#287](https://github.com/Vraxo/Formloom/actions/runs/42781659921) passed the Release build and all 844 tests (739 Core, 105 Desktop). Test results: [artifact 13745973608](https://github.com/Vraxo/Formloom/actions/runs/42781659921/artifacts/13745973608); procedural preview: [13746092750](https://github.com/Vraxo/Formloom/actions/runs/42781659921/artifacts/13746092750); legacy Phase 10 rose-review baseline: [13746247925](https://github.com/Vraxo/Formloom/actions/runs/42781659921/artifacts/13746247925). The evaluator handles the five current schema-v1 node types and bounded evaluation; it is not a complete rose implementation or renderer. The full rose knowledge/algorithm inventory, generic material/style contract, visual equivalence, generic vase-scene integration, and data-only second-model proof remain open. ADR-0001 is binding throughout.
