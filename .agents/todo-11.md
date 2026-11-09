# Phase 11 — Data-driven model system

> **Status:** In progress. The Phase 11 architecture contract and source audit are being established first. No model-definition loader, interpreter, generic geometry representation, or migrated model has been implemented yet.
>
> **Starting point:** `main` at `fc4e587166617d72d2be9679aadc5a160c79878e` (2026-11-09). The first implementation branch is `phase-11/model-system-contract`.

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
- [ ] Run the unmodified full-solution Release build and test suite on the baseline commit, or record the latest verified CI run if a local baseline cannot be run.
- [ ] Record the exact baseline test/CI run and the artifact IDs/paths used for subsequent comparisons.
- [ ] Confirm that all Phase 10 supported rose prompt strings, subject tokens, seed/variation cases, dimensions, silhouette behavior, and material variants are covered by the baseline tests.

### 1. Architecture contract and baseline documentation

**PR 1 — contract and audit.** Keep this change documentation-only unless a concrete documentation/build issue requires otherwise.

- [x] Write `docs/model-system.md` with the Level 2 boundary, architectural responsibilities, declarative-data rule, format direction, geometry/material boundary, determinism requirements, migration strategy, non-goals, acceptance criteria, and current source audit.
- [x] Add this Phase 11 checklist with PR-sized milestones and evidence-based acceptance gates.
- [ ] Review the contract against the actual rose implementation and ensure it does not assume operations the current engine has not demonstrated.
- [ ] Verify links and paths in the two new documents.
- [ ] Open a PR against `main`; keep it separate from model-system implementation work.
- [ ] Obtain a passing Linux / Release CI run for the documentation PR.
- [ ] Record the PR, commit, and CI run here. Do not mark implementation milestones complete from a passing docs-only build.

**Acceptance gate:** The team has a shared, repository-resident definition of Level 2 and a concrete source audit. No schema or operator API is frozen merely by this documentation PR.

### 2. Model format and typed definition design

**PR 2 — versioned definition contract and schema.**

- [ ] Re-read `docs/model-system.md` and enumerate the exact data required to represent the existing rose without invoking rose-specific code.
- [ ] Choose and document the versioned external serialization contract. JSON with `System.Text.Json` is the initial direction; validate it against the rose before freezing field names.
- [ ] Define stable, namespaced model IDs (for example, `flowers.rose`) and model definition schema versions.
- [ ] Define typed C# representation records for metadata, prompt descriptors, parameters, variants, parts, operation nodes, anchors, constraints, material slots, and variation ranges as actually required.
- [ ] Define the normalized coordinate system, coordinate spaces, units, transform order, bounds semantics, and parent/local reference rules.
- [ ] Define how a model declares material data without referencing SkiaSharp types.
- [ ] Define how definitions refer to other reusable templates/definitions, if the rose requires such reuse; reject unresolved and cyclic references.
- [ ] Define explicit resource limits for document size, node/depth counts, arrays, repetition counts, and numeric ranges.
- [ ] Include a representative rose fragment and a complete schema/format example in the documentation.
- [ ] Add format-level tests for valid documents, unsupported schema versions, missing/duplicate identifiers, malformed values, unknown fields/operators where relevant, and invalid references.
- [ ] Do not implement an expression language, embedded script, or arbitrary plugin mechanism.

**Acceptance gate:** The versioned format can describe the rose's static knowledge and intended construction declaratively. Its C# representation is typed; it is not a bag of untyped JSON values.

### 3. Runtime model catalog, loading, and validation

**PR 3 — external discovery and diagnostics.**

- [ ] Add a model catalog that discovers definitions from an explicit filesystem root at runtime.
- [ ] Support a documented default model directory and a user-supplied/custom model directory without requiring an application rebuild.
- [ ] Distribute built-in model files as ordinary files alongside the application rather than embedding object definitions in compiled C#.
- [ ] Make duplicate IDs, duplicate prompt descriptors, invalid schema versions, missing files, and malformed definitions observable as structured diagnostics.
- [ ] Validate references, operator names and parameter types, finite numeric values, ranges, graph depth/cycles, anchor existence, and resource limits before evaluation.
- [ ] Use the repository's Result/error-value conventions for expected file/definition errors; do not use exceptions as ordinary validation flow.
- [ ] Ensure the catalog can load a temporary test directory so tests can prove file-based discovery independently of the production model set.
- [ ] Test missing, malformed, invalid, duplicate, and valid model directories, including one newly added definition after catalog creation/reload according to the chosen lifecycle.
- [ ] Document filesystem discovery, model precedence, diagnostics, and reload/restart behavior.

**Acceptance gate:** A valid external definition is discoverable and a malformed one produces a useful diagnostic before any geometry is rendered.

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
| PR 1 | Architecture contract and source audit | In progress | `docs/model-system.md`, this checklist |
| PR 2 | Versioned model format and typed schema | Not started | — |
| PR 3 | Model catalog and definition validation | Not started | — |
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

**Current status:** The source audit is complete and the architecture contract is being added. Phase 11 implementation has not yet moved any rose construction into external data. The next action after PR 1 is to design the smallest versioned definition format that can represent the existing rose without retaining a species-specific generator.
