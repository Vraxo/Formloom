"""Procedural geometry and still-life scenes for the Formloom Blender experiment."""

import argparse
import json
import math
import os
import sys
import time

import bpy
from mathutils import Vector


STUDIO_FLOOR_HEIGHT = -1.085
APPLE_LOWEST_PROFILE_HEIGHT = -0.64
APPLE_DEFAULT_SCALE = (0.82, 0.78, 0.92)


def parse_arguments():
    arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--resolution", type=int, default=512)
    parser.add_argument("--samples", type=int, default=32)
    parser.add_argument("--denoise", choices=("true", "false"), default="false")
    parsed = parser.parse_args(arguments)
    parsed.denoise = parsed.denoise == "true"
    if not 1 <= parsed.samples <= 256:
        parser.error("--samples must be between 1 and 256")
    if not 64 <= parsed.resolution <= 2048:
        parser.error("--resolution must be between 64 and 2048")
    return parsed


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def create_cube():
    # Construct the mesh explicitly: this experiment tests Formloom-authored geometry,
    # not Blender's built-in cube primitive.
    vertices = [
        (-1.0, -1.0, -1.0), (1.0, -1.0, -1.0),
        (1.0, 1.0, -1.0), (-1.0, 1.0, -1.0),
        (-1.0, -1.0, 1.0), (1.0, -1.0, 1.0),
        (1.0, 1.0, 1.0), (-1.0, 1.0, 1.0),
    ]
    faces = [
        (0, 3, 2, 1), (4, 5, 6, 7),
        (0, 1, 5, 4), (1, 2, 6, 5),
        (2, 3, 7, 6), (3, 0, 4, 7),
    ]
    mesh = bpy.data.meshes.new("FormloomCubeMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    cube = bpy.data.objects.new("FormloomCube", mesh)
    bpy.context.collection.objects.link(cube)

    bevel = cube.modifiers.new("SoftenedEdges", "BEVEL")
    bevel.width = 0.075
    bevel.segments = 5
    bevel.limit_method = "ANGLE"
    bevel.harden_normals = True

    normals = cube.modifiers.new("WeightedCornerNormals", "WEIGHTED_NORMAL")
    normals.keep_sharp = True
    normals.weight = 40
    return cube


def principled_node(material):
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    shader = nodes.new("ShaderNodeBsdfPrincipled")
    material.node_tree.links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    return shader


def create_material(name, kind, treatment="baseline"):
    material = bpy.data.materials.new(name)
    shader = principled_node(material)
    shader.inputs["Metallic"].default_value = 0.0
    shader.inputs["Roughness"].default_value = 0.72

    if kind == "clay":
        shader.inputs["Base Color"].default_value = (0.62, 0.19, 0.075, 1.0)
    elif kind == "ceramic":
        shader.inputs["Base Color"].default_value = (0.045, 0.24, 0.48, 1.0)
        shader.inputs["Roughness"].default_value = 0.28
        coat = shader.inputs.get("Coat Weight") or shader.inputs.get("Clearcoat")
        if coat is not None:
            coat.default_value = 0.18
        if treatment == "illustrated":
            # A muted blue glaze, broad reflections, and restrained surface
            # variation avoid the hard, plastic-like product-render finish.
            shader.inputs["Base Color"].default_value = (0.055, 0.17, 0.29, 1.0)
            shader.inputs["Roughness"].default_value = 0.36
            if coat is not None:
                coat.default_value = 0.10
            coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
            glaze_noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
            glaze_noise.inputs["Scale"].default_value = 3.0
            glaze_noise.inputs["Detail"].default_value = 1.5
            glaze_ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
            glaze_ramp.color_ramp.elements[0].position = 0.22
            glaze_ramp.color_ramp.elements[0].color = (0.038, 0.12, 0.22, 1.0)
            glaze_ramp.color_ramp.elements[1].position = 0.78
            glaze_ramp.color_ramp.elements[1].color = (0.075, 0.23, 0.36, 1.0)
            links = material.node_tree.links
            links.new(coordinates.outputs["Generated"], glaze_noise.inputs["Vector"])
            links.new(glaze_noise.outputs["Fac"], glaze_ramp.inputs["Fac"])
            links.new(glaze_ramp.outputs["Color"], shader.inputs["Base Color"])
    elif kind == "apple":
        shader.inputs["Roughness"].default_value = 0.42
        coat = shader.inputs.get("Coat Weight") or shader.inputs.get("Clearcoat")
        if coat is not None:
            coat.default_value = 0.09

        coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
        noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 2.8
        noise.inputs["Detail"].default_value = 2.0
        ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.24
        ramp.color_ramp.elements[0].color = (0.25, 0.006, 0.012, 1.0)
        ramp.color_ramp.elements[1].position = 0.76
        ramp.color_ramp.elements[1].color = (0.62, 0.045, 0.018, 1.0)
        bump = material.node_tree.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.035
        bump.inputs["Distance"].default_value = 0.012

        links = material.node_tree.links
        links.new(coordinates.outputs["Generated"], noise.inputs["Vector"])
        links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
        links.new(noise.outputs["Fac"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], shader.inputs["Normal"])

        if treatment == "illustrated":
            # Softer satin skin, a warmer red/orange value range, and low-
            # contrast pigment variation avoid a polished CG product finish.
            shader.inputs["Roughness"].default_value = 0.58
            if coat is not None:
                coat.default_value = 0.025
            ramp.color_ramp.elements[0].color = (0.19, 0.012, 0.012, 1.0)
            ramp.color_ramp.elements[1].color = (0.50, 0.065, 0.026, 1.0)
            noise.inputs["Scale"].default_value = 3.4
            noise.inputs["Detail"].default_value = 1.35
            bump.inputs["Strength"].default_value = 0.012
            bump.inputs["Distance"].default_value = 0.005
    elif kind == "metal":
        shader.inputs["Base Color"].default_value = (0.48, 0.52, 0.58, 1.0)
        shader.inputs["Metallic"].default_value = 0.88
        shader.inputs["Roughness"].default_value = 0.28
    elif kind == "wood":
        coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
        mapping = material.node_tree.nodes.new("ShaderNodeMapping")
        mapping.inputs["Scale"].default_value = (1.5, 12.0, 12.0)
        wave = material.node_tree.nodes.new("ShaderNodeTexWave")
        wave.wave_type = "BANDS"
        wave.bands_direction = "X"
        wave.inputs["Scale"].default_value = 5.0
        wave.inputs["Distortion"].default_value = 3.0
        wave.inputs["Detail"].default_value = 3.0
        ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
        ramp.color_ramp.elements[0].position = 0.22
        ramp.color_ramp.elements[0].color = (0.055, 0.018, 0.006, 1.0)
        ramp.color_ramp.elements[1].position = 0.78
        ramp.color_ramp.elements[1].color = (0.48, 0.22, 0.075, 1.0)
        noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = 24.0
        bump = material.node_tree.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.12
        bump.inputs["Distance"].default_value = 0.035
        links = material.node_tree.links
        links.new(coordinates.outputs["Generated"], mapping.inputs["Vector"])
        links.new(mapping.outputs["Vector"], wave.inputs["Vector"])
        links.new(wave.outputs["Color"], ramp.inputs["Fac"])
        links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
        links.new(coordinates.outputs["Generated"], noise.inputs["Vector"])
        links.new(noise.outputs["Fac"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    else:
        raise ValueError("Unknown material: " + kind)

    return material


def create_ground(treatment="baseline"):
    # A 360-degree cyclorama replaces the finite floor plane. The raised,
    # rounded perimeter keeps distant studio horizons out of low camera views.
    floor_height = STUDIO_FLOOR_HEIGHT
    profile = [
        (14.0, floor_height),
        (14.7, floor_height + 0.01),
        (15.3, floor_height + 0.08),
        (15.9, floor_height + 0.28),
        (16.35, floor_height + 0.62),
        (16.62, floor_height + 1.08),
        (16.78, floor_height + 1.72),
        (16.82, floor_height + 2.8),
        (16.82, floor_height + 9.0),
    ]
    segments = 128
    vertices = [(0.0, 0.0, floor_height)]
    faces = []

    for radius, height in profile:
        for segment in range(segments):
            angle = 2.0 * math.pi * segment / segments
            vertices.append((
                radius * math.cos(angle),
                radius * math.sin(angle),
                height,
            ))

    for segment in range(segments):
        faces.append((
            0,
            1 + segment,
            1 + (segment + 1) % segments,
        ))

    for ring in range(len(profile) - 1):
        inner_start = 1 + ring * segments
        outer_start = inner_start + segments
        for segment in range(segments):
            next_segment = (segment + 1) % segments
            faces.append((
                inner_start + segment,
                outer_start + segment,
                outer_start + next_segment,
                inner_start + next_segment,
            ))

    mesh = bpy.data.meshes.new("StudioCycloramaMesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    ground = bpy.data.objects.new("StudioCyclorama", mesh)
    bpy.context.collection.objects.link(ground)
    for polygon in mesh.polygons:
        polygon.use_smooth = True

    material = bpy.data.materials.new("WarmNeutralCyclorama")
    shader = principled_node(material)
    shader.inputs["Base Color"].default_value = (0.255, 0.235, 0.215, 1.0)
    shader.inputs["Roughness"].default_value = 0.94
    if treatment == "illustrated":
        # Warm paper/linen neutral: a quiet, lighter field behind the objects.
        shader.inputs["Base Color"].default_value = (0.34, 0.30, 0.25, 1.0)
        shader.inputs["Roughness"].default_value = 0.98
    ground.data.materials.append(material)
    return ground


def create_illustrated_tabletop():
    # A shallow bevelled slab gives the composition a visible front edge and
    # real contact shadows instead of an infinite studio-floor treatment.
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, -0.8, STUDIO_FLOOR_HEIGHT - 0.12))
    tabletop = bpy.context.object
    tabletop.name = "IllustratedOakTabletop"
    tabletop.dimensions = (14.0, 11.0, 0.24)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = tabletop.modifiers.new("SoftTableEdge", "BEVEL")
    bevel.width = 0.075
    bevel.segments = 4
    bevel.harden_normals = True
    tabletop.modifiers.new("WeightedTableNormals", "WEIGHTED_NORMAL")

    material = bpy.data.materials.new("WarmIllustratedOak")
    shader = principled_node(material)
    shader.inputs["Roughness"].default_value = 0.76
    coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
    mapping = material.node_tree.nodes.new("ShaderNodeMapping")
    mapping.inputs["Scale"].default_value = (0.7, 18.0, 4.0)
    wave = material.node_tree.nodes.new("ShaderNodeTexWave")
    wave.wave_type = "BANDS"
    wave.bands_direction = "Y"
    wave.inputs["Scale"].default_value = 2.6
    wave.inputs["Distortion"].default_value = 5.0
    wave.inputs["Detail"].default_value = 3.0
    noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 8.0
    noise.inputs["Detail"].default_value = 2.0
    ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.18
    ramp.color_ramp.elements[0].color = (0.075, 0.027, 0.009, 1.0)
    ramp.color_ramp.elements[1].position = 0.82
    ramp.color_ramp.elements[1].color = (0.34, 0.15, 0.052, 1.0)
    bump = material.node_tree.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.045
    bump.inputs["Distance"].default_value = 0.012
    links = material.node_tree.links
    links.new(coordinates.outputs["Generated"], mapping.inputs["Vector"])
    links.new(mapping.outputs["Vector"], wave.inputs["Vector"])
    links.new(wave.outputs["Color"], ramp.inputs["Fac"])
    links.new(coordinates.outputs["Generated"], noise.inputs["Vector"])
    links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    tabletop.data.materials.append(material)
    return tabletop


def create_illustrated_backdrop():
    # A softly textured vertical wall replaces the seamless studio sweep.
    vertices = [
        (-12.0, 4.55, STUDIO_FLOOR_HEIGHT - 0.01),
        (12.0, 4.55, STUDIO_FLOOR_HEIGHT - 0.01),
        (12.0, 4.55, 9.0),
        (-12.0, 4.55, 9.0),
    ]
    mesh = bpy.data.meshes.new("WarmPlasterBackdropMesh")
    mesh.from_pydata(vertices, [], [(0, 1, 2, 3)])
    mesh.update()
    wall = bpy.data.objects.new("WarmPlasterBackdrop", mesh)
    bpy.context.collection.objects.link(wall)

    material = bpy.data.materials.new("WarmPlasterBackdropMaterial")
    shader = principled_node(material)
    shader.inputs["Base Color"].default_value = (0.38, 0.31, 0.235, 1.0)
    shader.inputs["Roughness"].default_value = 0.96
    coordinates = material.node_tree.nodes.new("ShaderNodeTexCoord")
    noise = material.node_tree.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 5.0
    noise.inputs["Detail"].default_value = 2.0
    ramp = material.node_tree.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.31, 0.255, 0.195, 1.0)
    ramp.color_ramp.elements[1].color = (0.42, 0.345, 0.265, 1.0)
    bump = material.node_tree.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.018
    bump.inputs["Distance"].default_value = 0.008
    links = material.node_tree.links
    links.new(coordinates.outputs["Generated"], noise.inputs["Vector"])
    links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], shader.inputs["Base Color"])
    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], shader.inputs["Normal"])
    wall.data.materials.append(material)
    return wall


def create_camera(location, resolution):
    data = bpy.data.cameras.new("ExperimentCamera")
    camera = bpy.data.objects.new("ExperimentCamera", data)
    bpy.context.collection.objects.link(camera)
    camera.location = location
    target = Vector((0.0, 0.0, -0.05))
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    data.type = "PERSP"
    data.lens = 52.0
    bpy.context.scene.camera = camera
    bpy.context.scene.render.resolution_x = resolution
    bpy.context.scene.render.resolution_y = resolution
    return camera


def create_lighting(treatment="baseline"):
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.28, 0.30, 0.34, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.36

    lights = [
        ("Key", (-3.8, -4.5, 6.0), 780.0, 5.5, (1.0, 1.0, 1.0)),
        ("Fill", (4.0, -1.0, 2.8), 300.0, 5.0, (1.0, 1.0, 1.0)),
        ("Rim", (1.0, 4.0, 4.5), 480.0, 4.5, (1.0, 1.0, 1.0)),
    ]
    if treatment == "illustrated":
        world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.30, 0.27, 0.24, 1.0)
        world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.30
        lights = [
            ("Key", (-3.8, -4.5, 6.0), 820.0, 6.8, (1.0, 0.86, 0.72)),
            ("Fill", (4.0, -1.0, 2.8), 245.0, 6.0, (0.78, 0.86, 1.0)),
            ("Rim", (1.0, 4.0, 4.5), 360.0, 5.5, (1.0, 0.91, 0.80)),
        ]
    for name, location, power, size, color in lights:
        data = bpy.data.lights.new(name, "AREA")
        data.energy = power
        data.color = color
        data.shape = "DISK"
        data.size = size
        light = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(light)
        light.location = location
        light.rotation_euler = (Vector((0.0, 0.0, 0.0)) - light.location).to_track_quat("-Z", "Y").to_euler()


def configure_render(resolution, samples, denoise):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = denoise
    if denoise:
        scene.cycles.denoiser = "OPENIMAGEDENOISE"
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.render.image_settings.color_depth = "8"
    scene.view_settings.view_transform = "AgX"
    scene.render.resolution_x = resolution
    scene.render.resolution_y = resolution
    scene.render.image_settings.compression = 15


def create_parametric_sphere(name, location, scale=(1.0, 1.0, 1.0), segments=48, rings=24):
    vertices = []
    faces = []
    for ring in range(rings + 1):
        latitude = -0.5 * math.pi + math.pi * ring / rings
        z = math.sin(latitude)
        radius = math.cos(latitude)
        for segment in range(segments):
            angle = 2.0 * math.pi * segment / segments
            vertices.append((
                radius * math.cos(angle) * scale[0],
                radius * math.sin(angle) * scale[1],
                z * scale[2],
            ))
    for ring in range(rings):
        for segment in range(segments):
            a = ring * segments + segment
            b = ring * segments + (segment + 1) % segments
            c = (ring + 1) * segments + (segment + 1) % segments
            d = (ring + 1) * segments + segment
            faces.append((a, b, c, d))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    obj.location = location
    return obj


def catmull_rom_component(p0, p1, p2, p3, amount):
    amount_squared = amount * amount
    amount_cubed = amount_squared * amount
    return 0.5 * (
        (2.0 * p1)
        + (-p0 + p2) * amount
        + (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * amount_squared
        + (-p0 + 3.0 * p1 - 3.0 * p2 + p3) * amount_cubed
    )


def interpolate_apple_profile(profile, subdivisions=8):
    samples = []
    for index in range(len(profile) - 1):
        p0 = profile[max(0, index - 1)]
        p1 = profile[index]
        p2 = profile[index + 1]
        p3 = profile[min(len(profile) - 1, index + 2)]
        for step in range(subdivisions):
            amount = step / subdivisions
            height = catmull_rom_component(p0[0], p1[0], p2[0], p3[0], amount)
            radius = catmull_rom_component(p0[1], p1[1], p2[1], p3[1], amount)
            samples.append((height, max(0.008, radius)))
    samples.append(profile[-1])
    return samples


def create_apple_stem(name, location, scale):
    cavity_location = (
        location[0],
        location[1],
        location[2] + 0.585 * scale[2],
    )
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=24,
        ring_count=12,
        radius=0.055,
        location=cavity_location,
    )
    cavity = bpy.context.object
    cavity.name = name + "StemWell"
    cavity.scale = (1.0, 1.0, 0.22)
    for polygon in cavity.data.polygons:
        polygon.use_smooth = True

    cavity_material = bpy.data.materials.new(name + "StemWellMaterial")
    cavity_shader = principled_node(cavity_material)
    cavity_shader.inputs["Base Color"].default_value = (0.075, 0.022, 0.012, 1.0)
    cavity_shader.inputs["Roughness"].default_value = 0.78
    cavity.data.materials.append(cavity_material)

    stem_curve = bpy.data.curves.new(name + "StemCurve", "CURVE")
    stem_curve.dimensions = "3D"
    stem_curve.resolution_u = 16
    stem_curve.bevel_depth = 0.025
    stem_curve.bevel_resolution = 4
    spline = stem_curve.splines.new("BEZIER")
    spline.bezier_points.add(2)
    stem_points = (
        (0.0, 0.0, 0.575 * scale[2]),
        (0.018 * scale[0], 0.004 * scale[1], 0.72 * scale[2]),
        (0.065 * scale[0], 0.018 * scale[1], 0.88 * scale[2]),
    )
    for point, coordinate, radius in zip(
        spline.bezier_points,
        stem_points,
        (1.0, 0.78, 0.52),
    ):
        point.co = coordinate
        point.radius = radius
        point.handle_left_type = "AUTO"
        point.handle_right_type = "AUTO"

    stem = bpy.data.objects.new(name + "Stem", stem_curve)
    bpy.context.collection.objects.link(stem)
    stem.location = location
    stem_material = bpy.data.materials.new(name + "StemMaterial")
    stem_shader = principled_node(stem_material)
    stem_shader.inputs["Base Color"].default_value = (0.16, 0.055, 0.018, 1.0)
    stem_shader.inputs["Roughness"].default_value = 0.68
    stem.data.materials.append(stem_material)


def create_apple(name, location, scale=(0.82, 0.78, 0.92), segments=96):
    # A hand-authored rotational profile replaces the earlier deformed sphere.
    # The narrow center caps form shallow blossom and stem wells; the broad
    # shoulders and five radial lobes establish an unmistakable fruit silhouette.
    profile = [
        # A shallow blossom dimple gives way almost immediately to a broad,
        # rounded base. The lower body stays full instead of collapsing to a
        # strawberry-like point.
        (-0.50, 0.015), (-0.56, 0.07), (-0.62, 0.18),
        (-0.64, 0.28), (-0.64, 0.36), (-0.61, 0.45),
        (-0.55, 0.52), (-0.43, 0.62), (-0.26, 0.71),
        (-0.06, 0.78), (0.16, 0.80),
        (0.34, 0.76), (0.48, 0.66), (0.54, 0.51),
        (0.55, 0.36), (0.51, 0.22), (0.48, 0.10), (0.47, 0.02),
    ]
    sampled_profile = interpolate_apple_profile(profile)
    vertices = []
    faces = []

    for height, base_radius in sampled_profile:
        upper_lobing = max(0.0, min(1.0, (height - 0.22) / 0.46))
        lower_lobing = max(0.0, min(1.0, (-height - 0.42) / 0.20))
        lobe_strength = max(upper_lobing, lower_lobing)
        for segment in range(segments):
            angle = 2.0 * math.pi * segment / segments
            five_lobes = math.cos(5.0 * angle + 0.35)
            subtle_irregularity = math.cos(3.0 * angle - 0.8)
            radial_variation = (
                0.032 * five_lobes + 0.008 * subtle_irregularity
            ) * lobe_strength
            vertical_variation = 0.012 * five_lobes * lobe_strength
            radius = base_radius * (1.0 + radial_variation)
            vertices.append((
                radius * math.cos(angle) * scale[0],
                radius * math.sin(angle) * scale[1],
                (height + vertical_variation) * scale[2],
            ))

    for ring in range(len(sampled_profile) - 1):
        for segment in range(segments):
            a = ring * segments + segment
            b = ring * segments + (segment + 1) % segments
            c = (ring + 1) * segments + (segment + 1) % segments
            d = (ring + 1) * segments + segment
            faces.append((a, b, c, d))

    # Close the tiny profile ends with caps rather than leaving open mesh rings.
    faces.append(tuple(reversed(range(segments))))
    top_start = (len(sampled_profile) - 1) * segments
    faces.append(tuple(top_start + segment for segment in range(segments)))

    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    apple = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(apple)
    apple.location = location
    for polygon in mesh.polygons:
        polygon.use_smooth = True

    create_apple_stem(name, location, scale)
    return apple


def create_grounded_apple(name, x, y):
    # The blossom end is recessed above a broad low ring; position that ring
    # against the studio floor instead of leaving the fruit hovering.
    location_z = STUDIO_FLOOR_HEIGHT - APPLE_LOWEST_PROFILE_HEIGHT * APPLE_DEFAULT_SCALE[2]
    return create_apple(name, (x, y, location_z), scale=APPLE_DEFAULT_SCALE)


def create_parametric_cylinder(name, location, radius=0.72, depth=2.0, segments=48):
    vertices = []
    faces = []
    for z in (-depth * 0.5, depth * 0.5):
        for segment in range(segments):
            angle = 2.0 * 3.141592653589793 * segment / segments
            vertices.append((radius * __import__("math").cos(angle), radius * __import__("math").sin(angle), z))
    faces.append(tuple(range(segments - 1, -1, -1)))
    faces.append(tuple(range(segments, segments * 2)))
    for segment in range(segments):
        next_segment = (segment + 1) % segments
        faces.append((segment, next_segment, segments + next_segment, segments + segment))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bevel = obj.modifiers.new("SoftenedEdges", "BEVEL")
    bevel.width = 0.045
    bevel.segments = 3
    bevel.harden_normals = True
    obj.location = location
    return obj


def create_lathed_vase(name, location):
    # A rotational profile gives us a controllable hollow vessel silhouette.
    import math
    profile = [
        (0.00, 0.72), (0.08, 0.78), (0.18, 0.82), (0.42, 0.76),
        (0.78, 0.58), (1.10, 0.43), (1.42, 0.40), (1.68, 0.47),
        (1.92, 0.62), (2.12, 0.68), (2.18, 0.66), (2.18, 0.55),
        (2.06, 0.54), (1.88, 0.51), (1.64, 0.37), (1.40, 0.31),
        (1.10, 0.34), (0.78, 0.48), (0.42, 0.64), (0.18, 0.68),
    ]
    segments = 64
    vertices = []
    faces = []
    for z, radius in profile:
        for segment in range(segments):
            angle = 2.0 * math.pi * segment / segments
            vertices.append((radius * math.cos(angle), radius * math.sin(angle), z))
    for ring in range(len(profile) - 1):
        for segment in range(segments):
            a = ring * segments + segment
            b = ring * segments + (segment + 1) % segments
            c = (ring + 1) * segments + (segment + 1) % segments
            d = (ring + 1) * segments + segment
            faces.append((a, b, c, d))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    vase = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(vase)
    vase.location = location
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    return vase


def assign_material(obj, material):
    obj.data.materials.clear()
    obj.data.materials.append(material)


def render_view(output_dir, filename, camera_location, resolution):
    camera = create_camera(camera_location, resolution)
    path = os.path.join(output_dir, filename)
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    if not os.path.isfile(path) or os.path.getsize(path) == 0:
        raise RuntimeError("Render output was not created: " + path)
    camera_data = camera.data
    bpy.context.scene.camera = None
    bpy.data.objects.remove(camera, do_unlink=True)
    bpy.data.cameras.remove(camera_data)
    return filename


def render_illustrated_still_life(output_dir, resolution):
    # Rebuild the same geometry and composition as the baseline still-life
    # render. Only material response, studio light colour/size, and cyclorama
    # tone differ, so the image pair isolates the art-direction treatment.
    clear_scene()
    vase = create_lathed_vase("IllustratedStillLifeVase", (-0.95, 0.45, -1.08))
    apple = create_grounded_apple("IllustratedStillLifeApple", 1.0, -0.35)
    vase_material = create_material("IllustratedCeramicGlaze", "ceramic", "illustrated")
    apple_material = create_material("IllustratedAppleSkin", "apple", "illustrated")
    assign_material(vase, vase_material)
    assign_material(apple, apple_material)
    create_illustrated_tabletop()
    create_illustrated_backdrop()
    create_lighting("illustrated")

    filename = "still-life-illustrated-treatment.png"
    camera_location = (4.6, -8.5, 4.0)
    started = time.perf_counter()
    render_view(output_dir, filename, camera_location, resolution)
    return {
        "file": filename,
        "scene": "still-life-art-direction-study",
        "treatment": "illustrated",
        "geometry": "same lathed vase and grounded apple as baseline",
        "environment": "bevelled procedural oak tabletop and subtly textured warm plaster wall",
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


def main():
    arguments = parse_arguments()
    os.makedirs(arguments.output_dir, exist_ok=True)
    clear_scene()
    cube = create_cube()
    create_ground()
    create_lighting()
    configure_render(arguments.resolution, arguments.samples, arguments.denoise)

    materials = ("clay", "ceramic", "metal", "wood")
    views = {
        "three-quarter": (4.2, -6.0, 3.8),
        "elevated": (4.2, -6.0, 7.0),
        "low": (5.8, -7.0, 1.6),
    }
    manifest = {
        "blender_version": bpy.app.version_string,
        "engine": "Cycles",
        "device": "CPU",
        "resolution": [arguments.resolution, arguments.resolution],
        "samples": arguments.samples,
        "denoising": arguments.denoise,
        "denoiser": "OPENIMAGEDENOISE" if arguments.denoise else None,
        "geometry": {
            "source": "explicit eight-vertex, six-face mesh",
            "bevel_width": 0.075,
            "bevel_segments": 5,
            "weighted_normals": True,
        },
        "renders": [],
    }

    for material_kind in materials:
        material = create_material("Cube_" + material_kind, material_kind)
        cube.data.materials.clear()
        cube.data.materials.append(material)
        for view_name, location in views.items():
            create_camera(location, arguments.resolution)
            filename = material_kind + "-" + view_name + ".png"
            path = os.path.join(arguments.output_dir, filename)
            bpy.context.scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            if not os.path.isfile(path) or os.path.getsize(path) == 0:
                raise RuntimeError("Render output was not created: " + path)
            manifest["renders"].append({
                "file": filename,
                "material": material_kind,
                "view": view_name,
            })
            camera = bpy.context.scene.camera
            camera_data = camera.data
            bpy.context.scene.camera = None
            bpy.data.objects.remove(camera, do_unlink=True)
            bpy.data.cameras.remove(camera_data)



    # Geometry gallery: the same camera/light/material pipeline now handles
    # curved surfaces and rotational geometry as well as the original cube.
    clear_scene()
    gallery_cube = create_cube()
    gallery_sphere = create_parametric_sphere("GallerySphere", (-2.15, 0.0, -0.205), scale=(0.88, 0.88, 0.88))
    gallery_cylinder = create_parametric_cylinder("GalleryCylinder", (2.15, 0.0, -0.16), radius=0.72, depth=1.85)
    gallery_material = create_material("GalleryClay", "clay")
    for obj in (gallery_cube, gallery_sphere, gallery_cylinder):
        assign_material(obj, gallery_material)
    create_ground()
    create_lighting()
    gallery_views = {
        "three-quarter": (5.0, -8.0, 4.0),
        "elevated": (5.0, -8.0, 7.0),
        "low": (6.0, -9.0, 1.8),
    }
    for view_name, location in gallery_views.items():
        filename = "geometry-gallery-" + view_name + ".png"
        render_view(arguments.output_dir, filename, location, arguments.resolution)
        manifest["renders"].append({
            "file": filename,
            "material": "clay",
            "view": view_name,
            "scene": "geometry-gallery",
            "geometry": ["explicit cube mesh", "parametric UV sphere", "parametric cylinder"],
        })

    # Inspect the fruit model on its own first, from angles that expose the
    # shoulder lobes, recessed stem well, and overall silhouette.
    clear_scene()
    apple = create_grounded_apple("AppleShapeStudy", 0.0, 0.0)
    apple_material = create_material("AppleSkin", "apple")
    assign_material(apple, apple_material)
    create_ground()
    create_lighting()
    apple_views = {
        "front": (0.0, -6.5, 1.8),
        "three-quarter": (3.8, -6.4, 2.2),
        "elevated": (3.8, -6.0, 4.8),
    }
    for view_name, camera_location in apple_views.items():
        filename = "apple-shape-" + view_name + ".png"
        render_view(arguments.output_dir, filename, camera_location, arguments.resolution)
        manifest["renders"].append({
            "file": filename,
            "material": "red apple skin",
            "view": view_name,
            "scene": "apple-shape-study",
            "geometry": [
                "interpolated rotational profile",
                "five-lobed shoulders and base",
                "recessed stem well",
                "curved tapered stem",
            ],
        })

    # Compare the exact same apple and camera at progressively higher sample
    # counts, then compare a denoised render at 64 samples. Timings are written
    # to the manifest so quality can be weighed against CPU cost.
    quality_study = []
    for sample_count in (12, 32, 64, 128):
        quality_study.append(render_quality_variant(
            arguments.output_dir,
            sample_count,
            False,
            arguments.resolution,
        ))
    if arguments.denoise:
        quality_study.append(render_quality_variant(
            arguments.output_dir,
            64,
            True,
            arguments.resolution,
        ))
    manifest["quality_study"] = quality_study
    manifest["renders"].extend(quality_study)

    configure_render(arguments.resolution, arguments.samples, arguments.denoise)

    # Reuse the same fruit mesh in a minimal still-life composition.
    clear_scene()
    vase = create_lathed_vase("StillLifeVase", (-0.95, 0.45, -1.08))
    apple = create_grounded_apple("StillLifeApple", 1.0, -0.35)
    vase_material = create_material("StillLifeGlazedCeramic", "ceramic")
    apple_material = create_material("StillLifeFruit", "apple")
    assign_material(vase, vase_material)
    assign_material(apple, apple_material)
    create_ground()
    create_lighting()
    filename = "still-life-vase-and-apple.png"
    render_view(arguments.output_dir, filename, (4.6, -8.5, 4.0), arguments.resolution)
    manifest["renders"].append({
        "file": filename,
        "material": "glazed ceramic and red apple skin",
        "view": "three-quarter",
        "scene": "still-life",
        "geometry": [
            "lathed vase profile",
            "custom interpolated apple profile",
            "five-lobed silhouette and recessed stem well",
            "curved tapered stem",
        ],
        "camera": [4.6, -8.5, 4.0],
    })

    illustrated_render = render_illustrated_still_life(arguments.output_dir, arguments.resolution)
    manifest["renders"].append(illustrated_render)
    manifest["art_direction_study"] = {
        "baseline": "still-life-vase-and-apple.png",
        "illustrated": illustrated_render["file"],
        "comparison": "same object geometry, object placement, camera, resolution, sample count, and denoising; illustrated materials, tabletop, backdrop, and lighting differ",
        "samples": arguments.samples,
        "denoising": arguments.denoise,
    }

    manifest_path = os.path.join(arguments.output_dir, "manifest.json")
    with open(manifest_path, "w", encoding="utf-8") as manifest_file:
        json.dump(manifest, manifest_file, indent=2)
        manifest_file.write("\n")
    print("FORMLOOM_BLENDER_CUBE_RENDER_COUNT=" + str(len(manifest["renders"])))


if __name__ == "__main__":
    main()
