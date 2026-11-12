"""Photorealistic still-life composition and render-quality variants."""

import time

import bpy

from geometry import assign_material, create_grounded_apple, create_lathed_vase
from materials import create_material, create_photorealistic_plaster_material, create_photorealistic_wood_material
from scene import clear_scene, create_illustrated_backdrop, create_illustrated_tabletop, create_photorealistic_lighting, render_view


def render_photorealistic_still_life(output_dir, resolution):
    # Preserve the reviewed geometry and framing while replacing the stylized
    # environment/material treatment with physically based surface detail and
    # a broad, directional studio-window lighting arrangement.
    clear_scene()
    vase = create_lathed_vase("PhotorealisticStillLifeVase", (-0.95, 0.45, -1.08))
    apple = create_grounded_apple("PhotorealisticStillLifeApple", 1.0, -0.35)
    vase_material = create_material("PhotorealisticCeramicGlaze", "ceramic")
    vase_shader = vase_material.node_tree.nodes.get("Principled BSDF")
    vase_shader.inputs["Roughness"].default_value = 0.22
    coat = vase_shader.inputs.get("Coat Weight") or vase_shader.inputs.get("Clearcoat")
    if coat is not None:
        coat.default_value = 0.28
    vase_noise = vase_material.node_tree.nodes.new("ShaderNodeTexNoise")
    vase_noise.inputs["Scale"].default_value = 68.0
    vase_noise.inputs["Detail"].default_value = 2.0
    vase_bump = vase_material.node_tree.nodes.new("ShaderNodeBump")
    vase_bump.inputs["Strength"].default_value = 0.035
    vase_bump.inputs["Distance"].default_value = 0.002
    vase_coordinates = vase_material.node_tree.nodes.new("ShaderNodeTexCoord")
    vase_links = vase_material.node_tree.links
    vase_links.new(vase_coordinates.outputs["Generated"], vase_noise.inputs["Vector"])
    vase_links.new(vase_noise.outputs["Fac"], vase_bump.inputs["Height"])
    vase_links.new(vase_bump.outputs["Normal"], vase_shader.inputs["Normal"])

    apple_material = create_material("PhotorealisticAppleSkin", "apple")
    apple_shader = apple_material.node_tree.nodes.get("Principled BSDF")
    apple_nodes = apple_material.node_tree.nodes
    apple_links = apple_material.node_tree.links

    # Scale the peel detail for the actual 384px output: the lenticels need to
    # occupy roughly one pixel, while epidermal relief should break up highlights
    # without reading as coarse bumps.
    apple_coordinates = apple_nodes.new("ShaderNodeTexCoord")
    apple_vector = apple_coordinates.outputs["Object"]

    pigment_noise = apple_nodes.new("ShaderNodeTexNoise")
    pigment_noise.inputs["Scale"].default_value = 5.5
    pigment_noise.inputs["Detail"].default_value = 3.0
    pigment_noise.inputs["Roughness"].default_value = 0.72
    pigment_ramp = apple_nodes.new("ShaderNodeValToRGB")
    pigment_ramp.color_ramp.elements[0].position = 0.22
    pigment_ramp.color_ramp.elements[0].color = (0.19, 0.004, 0.008, 1.0)
    pigment_ramp.color_ramp.elements[1].position = 0.78
    pigment_ramp.color_ramp.elements[1].color = (0.37, 0.022, 0.018, 1.0)
    apple_links.new(apple_vector, pigment_noise.inputs["Vector"])
    apple_links.new(pigment_noise.outputs["Fac"], pigment_ramp.inputs["Fac"])

    # Lenticels are small warm pinpricks, not large dots. At this scale the
    # centre and soft edge survive the final render without covering the peel.
    lenticels = apple_nodes.new("ShaderNodeTexVoronoi")
    lenticels.inputs["Scale"].default_value = 38.0
    lenticels.inputs["Randomness"].default_value = 1.0
    lenticel_ramp = apple_nodes.new("ShaderNodeValToRGB")
    lenticel_ramp.color_ramp.elements[0].position = 0.12
    lenticel_ramp.color_ramp.elements[0].color = (0.48, 0.20, 0.075, 1.0)
    lenticel_ramp.color_ramp.elements[1].position = 0.44
    lenticel_ramp.color_ramp.elements[1].color = (0.20, 0.006, 0.010, 1.0)
    lenticel_edge = lenticel_ramp.color_ramp.elements.new(0.25)
    lenticel_edge.color = (0.31, 0.035, 0.014, 1.0)

    lenticel_mix = apple_nodes.new("ShaderNodeMixRGB")
    lenticel_mix.blend_type = "MIX"
    lenticel_mix.inputs["Fac"].default_value = 0.30
    apple_links.new(apple_vector, lenticels.inputs["Vector"])
    apple_links.new(lenticels.outputs["Distance"], lenticel_ramp.inputs["Fac"])
    apple_links.new(pigment_ramp.outputs["Color"], lenticel_mix.inputs[1])
    apple_links.new(lenticel_ramp.outputs["Color"], lenticel_mix.inputs[2])
    apple_links.new(lenticel_mix.outputs["Color"], apple_shader.inputs["Base Color"])

    # The peel has a gently pebbled epidermis. Lower spatial frequency is
    # deliberate: sub-pixel noise was being erased by denoising at this size.
    epidermis_noise = apple_nodes.new("ShaderNodeTexNoise")
    epidermis_noise.inputs["Scale"].default_value = 68.0
    epidermis_noise.inputs["Detail"].default_value = 2.5
    epidermis_noise.inputs["Roughness"].default_value = 0.72
    epidermis_bump = apple_nodes.new("ShaderNodeBump")
    epidermis_bump.inputs["Strength"].default_value = 0.19
    epidermis_bump.inputs["Distance"].default_value = 0.0022
    apple_links.new(apple_vector, epidermis_noise.inputs["Vector"])
    apple_links.new(epidermis_noise.outputs["Fac"], epidermis_bump.inputs["Height"])

    # A shallow depression beneath each lenticel gives the freckles a little
    # physical definition instead of relying on colour contrast alone.
    lenticel_height = apple_nodes.new("ShaderNodeValToRGB")
    lenticel_height.color_ramp.elements[0].position = 0.12
    lenticel_height.color_ramp.elements[0].color = (0.0, 0.0, 0.0, 1.0)
    lenticel_height.color_ramp.elements[1].position = 0.44
    lenticel_height.color_ramp.elements[1].color = (1.0, 1.0, 1.0, 1.0)
    lenticel_bump = apple_nodes.new("ShaderNodeBump")
    lenticel_bump.inputs["Strength"].default_value = 0.12
    lenticel_bump.inputs["Distance"].default_value = 0.0010
    apple_links.new(lenticels.outputs["Distance"], lenticel_height.inputs["Fac"])
    apple_links.new(lenticel_height.outputs["Color"], lenticel_bump.inputs["Height"])
    apple_links.new(epidermis_bump.outputs["Normal"], lenticel_bump.inputs["Normal"])

    # Fine pores finish the surface, but remain subordinate to the epidermis.
    pore_noise = apple_nodes.new("ShaderNodeTexNoise")
    pore_noise.inputs["Scale"].default_value = 155.0
    pore_noise.inputs["Detail"].default_value = 2.0
    pore_bump = apple_nodes.new("ShaderNodeBump")
    pore_bump.inputs["Strength"].default_value = 0.07
    pore_bump.inputs["Distance"].default_value = 0.00045
    apple_links.new(apple_vector, pore_noise.inputs["Vector"])
    apple_links.new(pore_noise.outputs["Fac"], pore_bump.inputs["Height"])
    apple_links.new(lenticel_bump.outputs["Normal"], pore_bump.inputs["Normal"])
    apple_links.new(pore_bump.outputs["Normal"], apple_shader.inputs["Normal"])

    # Waxy but not glossy: slight local roughness changes soften the highlights.
    roughness_ramp = apple_nodes.new("ShaderNodeValToRGB")
    roughness_ramp.color_ramp.elements[0].color = (0.34, 0.34, 0.34, 1.0)
    roughness_ramp.color_ramp.elements[1].color = (0.50, 0.50, 0.50, 1.0)
    apple_links.new(epidermis_noise.outputs["Fac"], roughness_ramp.inputs["Fac"])
    apple_links.new(roughness_ramp.outputs["Color"], apple_shader.inputs["Roughness"])

    apple_coat = apple_shader.inputs.get("Coat Weight") or apple_shader.inputs.get("Clearcoat")
    if apple_coat is not None:
        apple_coat.default_value = 0.025
    assign_material(vase, vase_material)
    assign_material(apple, apple_material)

    tabletop = create_illustrated_tabletop()
    assign_material(tabletop, create_photorealistic_wood_material())
    backdrop = create_illustrated_backdrop()
    assign_material(backdrop, create_photorealistic_plaster_material())
    create_photorealistic_lighting()

    filename = "still-life-photorealistic-treatment.png"
    camera_location = (4.6, -8.5, 4.0)
    started = time.perf_counter()
    render_view(output_dir, filename, camera_location, resolution)
    return {
        "file": filename,
        "scene": "still-life-photorealism-study",
        "treatment": "photorealistic",
        "geometry": "same lathed vase and grounded apple as the baseline and illustrated comparison",
        "environment": "procedural natural oak tabletop and fine-textured warm plaster wall",
        "lighting": "large warm key, cool soft fill, restrained warm rim, and low neutral world fill",
        "camera": list(camera_location),
        "render_seconds": round(time.perf_counter() - started, 3),
    }

def render_quality_variant(output_dir, sample_count, denoise, resolution):
    scene = bpy.context.scene
    scene.cycles.samples = sample_count
    scene.cycles.use_denoising = denoise
    if denoise:
        scene.cycles.denoiser = "OPENIMAGEDENOISE"

    filename = f"apple-quality-{sample_count:03d}-samples"
    if denoise:
        filename += "-denoised"
    filename += ".png"

    started = time.perf_counter()
    render_view(output_dir, filename, (3.8, -6.4, 2.2), resolution)
    elapsed = time.perf_counter() - started
    return {
        "file": filename,
        "scene": "apple-quality-study",
        "samples": sample_count,
        "denoising": denoise,
        "render_seconds": round(elapsed, 3),
        "view": "three-quarter",
    }
