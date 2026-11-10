# ADR-0002: Establish a coherent default visual style

- **Status:** Accepted
- **Date:** 2026-11-10
- **Scope:** Default rendering direction and future style architecture

## Context

Formloom has accumulated useful rendering techniques—organic contours, gradients, highlights, procedural surface detail, strokes, and shadows—but they are implemented unevenly across object families. The result has no sufficiently coherent visual identity. It can read as an unfinished middle ground between flat vector graphics and realistic illustration.

The model-system work also creates a risk: material palettes and rendering effects could become mixed into model definitions without a clear distinction between object appearance and the shared visual treatment. The existing model-system contract describes material data and renderer capabilities, but it does not establish an art direction.

The current application renderer is still a collection of legacy object-specific rendering paths. The standalone generic model renderer is an experimental capability, not yet part of application generation.

## Decision

Formloom's default visual direction is **Naturalistic Still-Life Illustration**: dimensional, tactile, carefully composed, and believable in form, materials, and lighting, while remaining visibly an illustration rather than claiming to be a photograph.

The binding visual principles and review criteria live in [Visual direction and rendering style](../art-direction.md).

The architecture separates four responsibilities:

1. **Model definitions** describe object geometry, parts, local details, material identity/roles, and model-specific appearance variants.
2. **Scenes** describe composition, object relationships, environment, view/camera assumptions, and the light arrangement.
3. **Rendering styles** define the shared visual treatment of geometry, materials, light, texture, edges, tone, and shadows.
4. **Rendering backends** implement those treatments through reusable drawing and rasterization capabilities.

Consequently:

- A model definition must not contain Skia resources, shader code, or the global art direction.
- A style must not identify models or duplicate their geometry.
- Scene lighting must be shared consistently across all objects in a composition; individual object renderers must not invent unrelated lighting conventions.
- Reusable shading and material behavior belong in the rendering engine, not in per-model branches.
- Model-specific palette and material distinctions must remain meaningful under the shared style.

This is a direction and ownership decision, not a frozen runtime API. The current model format does not yet have a complete semantic material system, and the renderer does not yet expose selectable style profiles. Do not add a style ID to model JSON or design a large style schema until a concrete visual prototype demonstrates the required reusable controls.

## Realism boundary

The near-term target is naturalistic illustration, not photorealism. "Realistic" in this context means credible volume, material response, and consistent lighting within an intentionally illustrated image.

Photorealism remains a possible future research direction, but it is not promised as a small extension to the current 2D gradient renderer. A convincing general-purpose photorealistic mode may require a substantial change to geometry, camera, material, and lighting representation. Any such direction requires a separate feasibility decision and explicit quality/performance criteria.

A clean vector style may be considered as a later, deliberately distinct alternative. It is not the default target.

## Consequences

### Positive

- Visual quality becomes an explicit product goal rather than an incidental result of adding renderer features.
- Model geometry, scene composition, style, and backend responsibilities have a clear boundary.
- Future material and shading work can be judged against one shared direction across object families.
- Additional styles can be introduced without duplicating model definitions.

### Trade-offs

- A coherent style will require coordinated changes across existing object-specific renderers before the generic model pipeline is fully integrated.
- The first implementation may improve one representative scene before being generalized.
- The document intentionally leaves the runtime style API open until real rendered output establishes what it needs.

## Required review questions

Before adding a rendering or material capability, answer:

1. Which visible shortcoming in the art direction does it address?
2. Is it a model-specific material fact, a scene property, a reusable style behavior, or a backend implementation detail?
3. Does it improve the reference scene without making the lighting or material language less coherent elsewhere?
4. Can it be reused without model-ID dispatch?
5. Have fixed-seed before/after outputs been inspected at thumbnail and full resolution?
6. Are tests and visual evidence both appropriate to the change?

When in doubt, prioritize a coherent, attractive image over adding another rendering option.
