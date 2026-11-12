# ADR-0004: Make symbolic image construction the primary product

- **Status:** Accepted direction; implementation work required
- **Date:** 2026-11-12
- **Scope:** Product identity, architecture priorities, and future rendering work

## Context

Formloom's purpose is to explore image generation without machine learning. It should not become a prompt wrapper around Blender, a library of hand-authored finished scenes, or a catalogue of unrelated prompt-specific drawing routines.

The intended product is a **symbolic procedural image generator**. It accepts a description, resolves the described concepts and relationships into a structured scene, constructs objects from explicit model knowledge and reusable algorithms, composes the scene, and renders the result. Its knowledge and decisions should be inspectable, deterministic where appropriate, and editable without training a model.

The project already has meaningful foundations:

- a typed Core scene representation and deterministic named random streams;
- a growing procedural vocabulary for fruit, ceramics, flowers, and their relationships;
- a CPU Skia renderer and a working desktop generation path;
- an emerging external model-definition catalog, validator, evaluator, generic geometry representation, and generic model-path renderer.

However, the development effort recently drifted toward improving one hard-coded Blender apple and its surface material. That work did not advance the central product capability: understanding a scene description and constructing it from Formloom's own model knowledge. It also encouraged a misleading success criterion in which a good-looking Blender render could be mistaken for progress on the generator.

## Decision

**The symbolic image-construction engine—not a particular renderer or subject—is the primary product.**

The generation pipeline is conceptually:

```text
Text description
      |
      v
Symbolic prompt interpretation
      |
      v
Typed scene plan
(objects, attributes, relations, constraints)
      |
      v
Model and visual-knowledge resolution
      |
      v
Procedural construction
(geometry, parts, materials, variation)
      |
      v
Scene composition
(placement, scale, depth, lighting, occlusion)
      |
      v
Rendering backend
      |
      v
Image
```

These stages must have explicit contracts. The prompt layer must not draw pixels; model definitions must not contain executable scripts; model construction must not know about Skia or Blender; and a renderer must not recognize individual model IDs to decide how to draw them.

### 1. Symbolic interpretation is a first-class capability

The current strict prompt grammar is a useful safety boundary and a testable starting point, not the final definition of understanding.

The longer-term prompt system should resolve language into explicit concepts, properties, quantities, spatial relations, containment, ordering, and scene constraints. It should report ambiguity or unsupported concepts instead of silently dropping them. The first implementation need not understand arbitrary natural language: it should expand in deliberate, testable increments.

A prompt such as "a red apple in a blue bowl beside a white flower" should ultimately become a typed plan with three model references, color attributes, a containment relation, a neighboring placement relation, and a shared setting—not a special case named after that sentence.

### 2. Model knowledge is explicit and reusable

Model definitions describe object-specific facts: profiles, proportions, parts, attachment points, counts, material variants, and bounded variation. Reusable geometry and procedural algorithms remain ordinary typed C# code, in accordance with [ADR-0001](0001-hybrid-procedural-engine.md).

A new model that fits the existing engine vocabulary should be addable as data, discoverable at runtime, and usable without a model-specific production-code branch or application rebuild. A genuinely specialized algorithm may remain code-authored when that is the clearer and more robust choice.

The model system in [docs/model-system.md](../model-system.md) is therefore central to the product, not a side experiment.

### 3. Composition is more than putting objects on a table

The scene planner must eventually reason about object relationships and visual constraints: support/contact, containment, relative placement, scale, depth order, attachment, available space, and occlusion. These should be generic relationships and constraints, not an ever-growing collection of prompt-specific scene constructors.

This is a staged goal. The current typed relationships remain valid legacy capabilities while generic scene planning is developed; do not rewrite them all at once.

### 4. Rendering is an interchangeable execution stage

The product is not defined by Blender, Skia, 2D, or 3D. A backend is useful only insofar as it renders the engine's scene and model representations faithfully.

- The current Skia CPU renderer remains operational and must not be casually removed.
- Blender may be used as an optional development/evaluation backend. It must not be required by Formloom users, become the source of model knowledge, or be used to manually author a finished subject and pass that off as Formloom generation.
- No downloaded meshes, stock images, pre-rendered artwork, or external textures are part of the model-generation path. Formloom constructs its visible content from its own geometry, algorithms, and authored model data.
- Backend-specific features must not leak into Core or the symbolic scene/model contracts.

This supersedes ADR-0003's **3D-first development priority**. The experiments and evidence recorded there remain historical and useful, but they do not determine the product roadmap. A 3D backend can be reconsidered when a concrete engine capability requires it; it is not the objective.

### 5. The visual target remains naturalistic illustration

The accepted Naturalistic Still-Life Illustration direction remains in [docs/art-direction.md](../art-direction.md). The goal is coherent, attractive, dimensional illustration—not photorealism at any cost. A stronger symbolic model and scene engine should improve the image because it constructs better forms and compositions, not because it accumulates noise, gloss, or manually tuned per-subject effects.

## Immediate implementation priority

The next milestone is a **single genuine end-to-end symbolic generation path**, not another isolated model or render study.

It must connect:

1. A model definition discovered from the runtime catalog.
2. Prompt descriptors resolved through that catalog rather than a model-specific parser branch.
3. A typed scene plan that refers to the model and its appearance choices.
4. A generic construction/evaluation path producing model-independent geometry.
5. A generic scene/render path that places and draws the result.
6. The existing deterministic seed, variation, diagnostics, desktop preview, and PNG save behavior.

Use the existing rose probe and generic geometry evaluator as implementation evidence, not as proof that this pipeline already exists. The first integrated model should be the rose because its current code provides a demanding behavioral and visual baseline. Keep its legacy path until the replacement has equivalent tests and reviewed output. Do not add the sunflower or another model merely to increase the object count before this end-to-end path works.

The milestone is accepted only when a user enters a supported prompt in the actual Formloom application and the resulting image is generated through the symbolic model pipeline. Tests must demonstrate the path and its deterministic behavior; the actual rendered PNG must be inspected. Passing CI or rendering a fixture in isolation is insufficient.

## Explicit non-goals

- Turning Formloom into a neural model, LLM wrapper, or hosted inference client.
- Claiming open-ended natural-language understanding before it exists.
- Replacing the engine with a universal JSON scripting language.
- Making Blender or any external asset a runtime requirement.
- Pursuing photorealism as the immediate acceptance target.
- Rewriting every existing object and relationship in one migration.
- Adding more prompt examples that each require a new hard-coded parser/generator/render branch.

## Consequences

This decision changes what counts as progress. A new apple material, a higher sample count, or another Blender scene is not a product milestone by itself. Work should be prioritized by whether it advances symbolic interpretation, reusable model knowledge, scene composition, or faithful rendering of those representations.

Existing code is not discarded simply because it predates this architecture. It is evidence and a source of reusable knowledge. Migrate it behind generic contracts incrementally, preserving working behavior and visual baselines until the replacement is proven.

Every milestone should answer: **What new kind of scene can Formloom understand and construct now that it could not construct before, and where is the evidence in the actual generated image?**
