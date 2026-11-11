# ADR-0003: Explore a 3D-first rendering path

- **Status:** Accepted exploratory direction; production backend not selected
- **Date:** 2026-11-11
- **Scope:** New rendering research and the evolution of Formloom's scene/rendering architecture

## Context

Formloom's production application currently constructs scenes in Core and renders them through the Skia CPU backend. That path has accumulated substantial object-specific work, but the user-reviewed images still show limitations in dimensional form, coherent lighting, and materially convincing surfaces.

A separate Blender experiment has established a second rendering path without touching production:

- PR [#90](https://github.com/Vraxo/Formloom/pull/90) constructed a cube mesh directly and rendered it with multiple materials and camera angles.
- PR [#91](https://github.com/Vraxo/Formloom/pull/91) added parametric curved primitives, a lathed vase, and a first combined scene.
- PR [#92](https://github.com/Vraxo/Formloom/pull/92) replaced the sphere placeholder with an authored apple profile, shoulders, stem well, and stem.
- PR [#93](https://github.com/Vraxo/Formloom/pull/93) corrected the apple's lower silhouette and grounding.
- PR [#94](https://github.com/Vraxo/Formloom/pull/94) added a controlled sample-count comparison, OpenImageDenoise, improved studio environment, and material refinements.

The resulting 3D images have been reviewed by the user and judged substantially more convincing than the current 2D output. This is enough evidence to change which path should receive new rendering-development effort. It is not yet evidence of a complete production renderer: the experiment has only a small set of objects, limited composition complexity, a basic material vocabulary, and CPU-only performance observations from CI.

The visual target remains the accepted **Naturalistic Still-Life Illustration** art direction in [docs/art-direction.md](../art-direction.md) and [ADR-0002](0002-visual-style-and-rendering-separation.md). The rendering representation is changing direction; the intended image is not changing to photorealism.

## Decision

Formloom will pursue a **3D-first path for new rendering research and future production architecture**.

1. **Treat 3D as the leading implementation direction.** New experiments should prioritize three-dimensional geometry, camera, lighting, material response, depth, and occlusion when those capabilities address visible shortcomings in the 2D output.
2. **Keep the model/scene/style/backend boundary.** ADR-0001's hybrid procedural-engine decision and ADR-0002's separation of model definitions, scenes, styles, and rendering backends remain in force. Models describe object identity and geometry; scenes arrange and light objects; styles define the visual treatment; backends execute it.
3. **Keep Blender isolated for now.** Blender/Cycles is the current evaluation backend and image-generation tool for experiments. It is not a dependency of Formloom Core, Desktop, or the current application runtime. The experiment's C# launcher invokes Blender as an external process.
4. **Do not lock the production backend prematurely.** A production renderer could use Blender or a different backend. The scene and model contracts should not unnecessarily expose Blender-specific runtime objects, node graphs, or Python APIs.
5. **Preserve the existing 2D renderer.** The Skia implementation remains the production path and a visual/behavioral reference. Do not remove it or perform a broad migration until the 3D path has demonstrated representative object coverage, multi-object composition, deterministic seeded variation, correct contact and occlusion, the required art direction, and acceptable CPU cost. Keeping Skia as an alternate backend/style remains an option.
6. **Keep quality continuously controllable.** Sample count remains an integer render setting rather than a selection among arbitrary named quality presets. Denoising is a separate configurable rendering operation; it must not conceal poor geometry, lighting, or materials.
7. **Keep photorealism separate.** Better 3D volume and material response support the illustrated target. They do not create a commitment to photographic output, GPU rendering, or a physically exact universal material system.

## Consequences

### Positive

- New effort can address geometry, depth, material response, shadows, and occlusion in one coherent scene representation.
- The existing 2D application remains usable while the alternative path matures.
- Blender provides a fast, scriptable environment for evaluating models, cameras, lights, materials, and rendering quality.
- A later backend decision can be based on representative output and measured resource use rather than preference alone.

### Trade-offs and risks

- A full 3D scene has more geometry, transform, camera, material, and occlusion concerns than the current 2D drawing model.
- CPU Cycles rendering can become expensive as resolution, samples, material complexity, and scene size grow.
- Blender's data model and Python API are useful for experiments but should not leak into Core or become the assumed public scene schema.
- The 2D and 3D outputs may coexist for an extended period; visual and behavioral parity is not free.
- A 3D render can still look synthetic or plastic. Art direction, composition, texture scale, and material restraint remain essential.

## Stages and exit criteria

### Stage A — prove the rendering workflow

**Current status: established as a prototype.**

Cube, curved primitives, an apple, a vase, camera positions, several materials, a studio setup, configurable samples, and denoising have been demonstrated in CI artifacts and reviewed. This stage does not establish production readiness.

### Stage B — prove the visual direction

**Next focus.**

- Develop a restrained illustrated appearance from the same reusable 3D scene.
- Compare material, lighting, and surface treatments on the apple and ceramic vessel.
- Add enough geometry variation to test more than one apple shape and at least one further organic form.
- Preserve fixed camera, seed, resolution, and render settings for meaningful before/after image review.
- Verify that the illustrated treatment works at thumbnail and full resolution without relying on universal heavy outlines.

**Exit:** the user accepts a coherent 3D still-life image whose form, materials, lighting, and background align with the documented art direction.

### Stage C — prove representative model and scene coverage

- Add and review a flower with leaves and a genuinely layered bloom, including attachment to a vase.
- Test liquid in a cup, vessel cavities, object grounding, object-to-object contact, and near/far occlusion.
- Demonstrate deterministic, bounded variation and multiple-object composition.
- Record CPU render time and memory at representative output sizes and sample counts.

**Exit:** the path can produce the main existing still-life subject families without per-image hand editing or model-ID-specific renderer dispatch.

### Stage D — define production boundaries

- Derive a backend-agnostic scene representation from requirements proven in Stages B and C.
- Decide whether Blender is appropriate as a production runtime or whether its role should remain development/reference rendering.
- Define how Core model data and deterministic seeds map to scene construction without importing Blender dependencies into Core.
- Compare the 3D path to the preserved Skia output and identify which 2D capabilities remain valuable as an alternate renderer or style.

**Exit:** there is a reviewed, testable integration proposal with quality, performance, portability, and maintenance evidence.

No production migration is authorized by this ADR alone. Each stage should remain a small, reviewable increment, and the 2D renderer stays intact until a separate integration decision is made.

## Required review questions

Before expanding the experiment or changing the architecture, answer:

1. Does the new scene element demonstrate a reusable 3D capability or only hard-code one model?
2. Are geometry, material identity, scene placement, lighting, style, and backend still separated?
3. Is the image better because of improved form and light, or merely because of added gloss, noise, or detail?
4. Does the output remain recognizably illustrated rather than drifting toward generic plastic CGI?
5. Are sample count, denoising, resolution, render time, and output dimensions recorded?
6. Has the actual PNG artifact been inspected at thumbnail and full resolution?
7. Does the change preserve the deterministic and bounded behavior expected of Formloom?

