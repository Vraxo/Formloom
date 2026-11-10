# Visual direction and rendering style

**Status:** Accepted art direction; implementation is incomplete  
**Decision date:** 2026-11-10  
**Applies to:** Default Formloom output and future rendering-style work

## 1. Direction

Formloom's default output should be a **naturalistic still-life illustration**: dimensional, tactile, carefully composed, and convincing in its forms, materials, and lighting, while remaining visibly authored as an illustration rather than pretending to be a photograph.

The intended result should feel like a finished piece of visual art—not a neutral collection of procedurally filled shapes. "Illustrated" is not permission for generic, flat, or unfinished output. The image should be attractive at a glance and reward closer inspection.

This direction is deliberately between two different goals:

- It should be substantially more lifelike and materially convincing than the current uneven, soft-shaded procedural look.
- It should retain the control, clarity, and intentional simplification of illustration instead of pursuing photographic imitation at any cost.

For now, **Naturalistic Still-Life Illustration** names the art direction, not a selectable runtime style ID or a promise that the current renderer already achieves it.

## 2. What exists today

The current renderer has useful ingredients, but they have grown around individual object implementations:

- Fruit uses organic contours, radial body shading, highlights, seeded surface marks, and cast/contact shadows.
- Ceramics use glaze gradients, highlights, cavities, rims, and procedural mottling.
- Flowers use species-specific contours, palettes, gradients, folds, veins, and selective strokes.
- The setting uses a gradient background, procedural wood grain, knots, and soft shadows.
- The standalone model-geometry renderer now supports solid fills, linear gradients, authored strokes, detail omission, and silhouettes. It is not yet the application's generation renderer.

These techniques do not yet form one coherent visual language. Light and shading are authored differently between object families; material identities are not consistently expressed; texture and edge treatment vary; and the main application still uses its legacy per-object renderer. The model-system work has improved the generic geometry foundation, but it has not unified the visual result.

The desktop antialiasing toggle is a rasterization choice, not a visual style. There is no general style selector or completed style-profile system today.

## 3. Principles for the default look

### Form before decoration

- Each object must have a recognizable, intentional silhouette before surface detail is added.
- Forms should communicate volume through their contours, overlaps, shading, and cast shadows.
- Organic objects should have controlled asymmetry and variation. Avoid both mechanically perfect shapes and random distortion.
- Important structures must remain readable at thumbnail size; fine detail should reward full-size inspection rather than carry the object's identity.

### One believable lighting arrangement

- A scene supplies the light arrangement. Every object in that scene must respond consistently to it.
- Shading should follow the object's form and material, not use gradients merely because a gradient is easy to draw.
- Use a coherent key light, restrained ambient fill, and contact/cast shadows that explain where objects meet the table, vessel, or one another.
- Highlights should have plausible placement, shape, and softness for the surface. They should not appear as arbitrary white blobs or identical glossy streaks on every material.
- Keep enough tonal separation to read form without crushing shadows or making every edge equally dark.

### Materials must look different for a reason

Material response is a major part of the intended style. Color alone is insufficient.

- **Fruit skin:** primarily diffuse or softly satin, with restrained highlights and subtle, scale-appropriate surface variation. An orange may have fine pores; an apple should not inherit orange-like noise.
- **Glazed ceramic:** smooth, dense, and reflective, with broad controlled glaze highlights, convincing rim and cavity depth, and restrained glaze variation. It should not look like opaque plastic.
- **Petals:** soft, layered, and slightly irregular, with gentle changes in value across their surfaces, visible overlap, and delicate folds. Avoid treating every petal as a flat cut-out with a dark outline.
- **Leaves and stems:** directional form shading and selective veins or ridges. Detail should reinforce the leaf's structure, not become decorative noise.
- **Wood:** directional grain that follows a coherent flow and remains subordinate to the objects. Large knots and fibers should have plausible scale and placement.

These are visual targets, not a commitment to simulate every optical property physically. A simplified effect is acceptable when it is coherent, material-specific, and visually convincing.

### Texture has a hierarchy

- Establish large-scale form and shading first, medium-scale material character second, and fine detail last.
- Detail density, frequency, and contrast must suit the material and output resolution.
- Randomness must be seeded and bounded, and should produce plausible material variation rather than uniform speckling.
- Noise is not a substitute for geometry, lighting, or material response. If removing texture makes an object look unfinished, its underlying form and shading need work.

### Edges should be intentional

- The default look is not defined by a universal dark outline around every object and part.
- Use contour contrast, overlap, shading, and selective detail strokes to define form.
- Reserve explicit linework for features that benefit from it, such as a leaf vein or a restrained petal fold.
- Stroke width, opacity, and edge contrast should be consistent with the image scale and material.

### A composed image, not a parts catalogue

- Objects, background, and tabletop should share a controlled palette and value structure.
- The setting should support the subject and provide depth without competing for attention.
- Overlaps, spacing, visual weight, and focal emphasis should feel deliberate.
- The final image should read as one still-life composition, not several independently rendered objects placed on a canvas.

## 4. Separate model, scene, style, and renderer responsibilities

The desired visual coherence depends on keeping four concerns distinct.

| Concern | Owns | Should not own |
|---|---|---|
| **Model definition** | Object geometry, proportions, parts, local details, material identity/roles, and model-specific appearance variants | Skia objects, shader code, global lighting, or the application's overall art direction |
| **Scene** | Object relationships, placement, composition, environment, view/camera assumptions, and the light arrangement | Species-specific rendering methods or a duplicated copy of each model's material implementation |
| **Rendering style** | The shared visual treatment: shading approach, material response, highlight character, texture character, edge treatment, tonal contrast, and shadow presentation | Object construction, model identity, arbitrary executable scripts, or renderer-specific resources |
| **Rendering backend** | Executing the style using reusable drawing, shading, texture, compositing, and rasterization capabilities | Per-model visual dispatch where a shared treatment is sufficient |

The model says **what the object is**. The scene says **how it is arranged and lit**. The style says **how that scene should look**. The backend makes the pixels.

A model may provide material variants and palette tokens. A style interprets those values through shared material behavior; it should not require a duplicate model definition for every style. Conversely, a style must not erase meaningful model-specific material differences.

The current model schema and renderer do not yet implement this separation completely. In particular, the current material tokens are not a complete semantic material system, and there is no runtime style-profile contract. Do not add a style ID or a large collection of style properties to the model JSON as a shortcut. Design any future style representation around concrete rendering needs and this boundary.

## 5. Realism and other possible styles

### Naturalistic illustration is the near-term target

For this direction, "realistic" means believable form, lighting, and material character—not literal photographic reproduction. A successful result may simplify geometry, texture, and optical behavior as long as the simplification is consistent and intentional.

This is a suitable goal for the current 2D procedural approach. It requires better shared rendering capabilities and more coherent art direction, not a claim that a few gradients make the output photorealistic.

### Photorealism is a separate, longer-term research direction

Photorealism means more than adding finer texture, stronger highlights, or extra shading passes. It implies convincing camera-like results across geometry, materials, light transport, reflections, transparency, shadows, and sampling.

A restricted photorealistic result may be possible with increasingly sophisticated 2D construction, and an offline CPU-first approach is not ruled out. However, a broadly convincing photorealistic mode would likely require a substantial expansion or change to the scene and rendering representation—potentially including volumetric or 2.5D/3D geometry and a more physically based renderer.

Therefore:

- Do not label the current or near-term 2D style "photorealistic."
- Do not promise a photorealistic mode as a small extension of the current gradient renderer.
- Keep photorealism open as a future feasibility study, with explicit quality, performance, and architectural criteria.
- Do not let that longer-term ambition delay making the default illustrated output genuinely beautiful.

### Other styles should be intentionally distinct

A clean vector style could be worthwhile later, but it should be a coherent alternative with its own deliberate shape, palette, edge, and shading rules—not the current renderer with gradients removed.

Add another style only when there is a clear visual purpose and a representative output that demonstrates the difference. Do not build a large style catalogue before one style is convincingly implemented.

## 6. Visual review and acceptance

Rendering changes must be evaluated as images as well as code. A capability that passes pixel tests can still produce an unattractive or incoherent result.

Use fixed prompts, seeds, variation, and dimensions for before/after comparisons. The existing default seed and variation are useful canonical settings where applicable. Review more than one material family before declaring a shared rendering improvement successful.

The initial reference set should include:

- A red apple on the wooden table: organic fruit form, skin response, highlight, surface detail, and contact/cast shadow.
- A red rose in a blue vase: layered petals, delicate floral detail, leaves, glazed ceramic, shared lighting, and vase/plant contact.
- A still beverage in a ceramic cup: liquid surface, reflections, vessel cavity, and material contrast.

For each reviewed image, ask:

1. Does the subject read immediately from its silhouette and composition?
2. Does the form have convincing volume without relying on outlines?
3. Does the lighting make sense across every object and the environment?
4. Can the materials be distinguished by their response, not just their base colors?
5. Is detail appropriate in scale, density, contrast, and placement?
6. Does the background support the focal subject?
7. Does the image feel like one intentional artwork rather than a set of unrelated rendering techniques?

Review both a reduced view and the full-resolution image. Inspect transparent diagnostic/silhouette outputs against a suitable background. Record any intentional visual changes rather than treating every difference from the legacy renderer as a regression.

Automated tests should separately cover deterministic output, material/geometry contracts, supported render options, and relevant pixel behavior. Tests do not replace visual review; visual review does not replace tests.

## 7. Implementation sequence

1. Keep this document as the art-direction authority while the model-system architecture continues to evolve.
2. Establish fixed reference renders and document the current baseline before broad visual changes. The initial user-captured apple and rose baselines, exact prompts, settings, and capture limitations are recorded in [the canonical render-baseline manifest](render-baselines.md) and [its machine-readable manifest](render-baselines.json).
3. Choose one representative scene and prove a coherent shared lighting/material treatment on it. The rose in a blue vase is a useful first stress case; the apple/table scene must also be checked before claiming the treatment is general.
4. Extract only the reusable rendering capabilities demonstrated by those outputs. Keep model-specific geometry and semantic material choices separate from the style implementation.
5. Review the same treatment across the reference set before defining a runtime style-profile API or introducing additional styles.
6. Integrate the generic model renderer into application generation only after the relevant geometry, material, scene, and visual requirements are sufficiently demonstrated.

This sequence does not require a full rendering rewrite in one step. Preserve working legacy behavior until replacements have automated coverage and reviewed visual evidence.

## 8. Non-goals

This decision does not:

- Claim that the current renderer already meets the target.
- Specify a final JSON schema, style ID, style-selector UI, or public style customization API.
- Require a universal physically based material system or physically exact simulation.
- Commit to photorealism, 3D rendering, GPU use, or replacing SkiaSharp.
- Require every existing object family to be migrated in one batch.
- Justify model-specific renderer branches or a general-purpose shading/scripting language.

The success criterion is not the number of rendering options. It is a coherent, attractive default image whose forms, materials, lighting, and composition work together.
