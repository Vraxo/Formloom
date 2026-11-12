"""Photorealistic still-life composition and render-quality variants."""

import time

import bpy

from geometry import assign_material, create_grounded_apple, create_lathed_vase
from materials import create_material, create_photorealistic_apple_material, create_photorealistic_plaster_material, create_photorealistic_wood_material
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

    apple_material = create_photorealistic_apple_material()
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
