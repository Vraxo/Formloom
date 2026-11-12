"""Shared Blender scene, camera, lighting, and render setup."""

import math
import os

import bpy
from mathutils import Vector

from geometry import STUDIO_FLOOR_HEIGHT
from materials import principled_node


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)

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
    # Keep the near edge at the same position, but extend the slab well past
    # the camera's left, right, and far framing. The backdrop hides the far
    # extension; no cyclorama surface can peek through at the image corners.
    tabletop_front_edge = -6.3
    tabletop_depth = 20.0
    tabletop_center_y = tabletop_front_edge + tabletop_depth * 0.5
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(0.0, tabletop_center_y, STUDIO_FLOOR_HEIGHT - 0.12),
    )
    tabletop = bpy.context.object
    tabletop.name = "IllustratedOakTabletop"
    tabletop.dimensions = (30.0, tabletop_depth, 0.24)
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

def create_camera(location, resolution, target=(0.0, 0.0, -0.05), lens=52.0):
    data = bpy.data.cameras.new("ExperimentCamera")
    camera = bpy.data.objects.new("ExperimentCamera", data)
    bpy.context.collection.objects.link(camera)
    camera.location = location
    target = Vector(target)
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()
    data.type = "PERSP"
    data.lens = lens
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

def create_photorealistic_lighting():
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.32, 0.36, 0.42, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.30
    lights = [
        # A broad window source gives the objects soft, readable form shadows
        # instead of the hard-edged studio/product-light look.
        ("LargeWindowKey", (-3.8, -4.5, 6.0), 1050.0, 7.5, (1.0, 0.93, 0.84)),
        ("SoftSkyFill", (4.0, -1.0, 3.0), 300.0, 7.0, (0.82, 0.88, 1.0)),
        # Keep a gentle separation highlight, not a bright outline.
        ("WarmBackRim", (1.0, 4.0, 4.5), 180.0, 6.0, (1.0, 0.91, 0.80)),
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
