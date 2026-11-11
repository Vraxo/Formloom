"""Procedural cube scene used to evaluate Blender as a Formloom rendering backend."""

import argparse
import json
import os
import sys

import bpy
from mathutils import Vector


def parse_arguments():
    arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--resolution", type=int, default=512)
    parser.add_argument("--samples", type=int, default=32)
    return parser.parse_args(arguments)


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


def create_material(name, kind):
    material = bpy.data.materials.new(name)
    shader = principled_node(material)
    shader.inputs["Metallic"].default_value = 0.0
    shader.inputs["Roughness"].default_value = 0.72

    if kind == "clay":
        shader.inputs["Base Color"].default_value = (0.62, 0.19, 0.075, 1.0)
    elif kind == "ceramic":
        shader.inputs["Base Color"].default_value = (0.045, 0.24, 0.48, 1.0)
        shader.inputs["Roughness"].default_value = 0.2
        coat = shader.inputs.get("Coat Weight") or shader.inputs.get("Clearcoat")
        if coat is not None:
            coat.default_value = 0.28
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


def create_ground():
    bpy.ops.mesh.primitive_plane_add(size=200.0, location=(0.0, 0.0, -1.085))
    ground = bpy.context.object
    ground.name = "Ground"
    material = bpy.data.materials.new("WarmNeutralGround")
    shader = principled_node(material)
    shader.inputs["Base Color"].default_value = (0.19, 0.17, 0.15, 1.0)
    shader.inputs["Roughness"].default_value = 0.86
    ground.data.materials.append(material)
    return ground


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


def create_lighting():
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.22, 0.25, 0.31, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.28

    lights = [
        ("Key", (-3.8, -4.5, 6.0), 850.0, 4.0),
        ("Fill", (4.0, -1.0, 2.8), 320.0, 3.5),
        ("Rim", (1.0, 4.0, 4.5), 600.0, 3.0),
    ]
    for name, location, power, size in lights:
        data = bpy.data.lights.new(name, "AREA")
        data.energy = power
        data.shape = "DISK"
        data.size = size
        light = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(light)
        light.location = location
        light.rotation_euler = (Vector((0.0, 0.0, 0.0)) - light.location).to_track_quat("-Z", "Y").to_euler()


def configure_render(resolution, samples):
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.render.image_settings.color_depth = "8"
    scene.view_settings.view_transform = "AgX"
    scene.render.resolution_x = resolution
    scene.render.resolution_y = resolution
    scene.render.image_settings.compression = 15


def main():
    arguments = parse_arguments()
    os.makedirs(arguments.output_dir, exist_ok=True)
    clear_scene()
    cube = create_cube()
    create_ground()
    create_lighting()
    configure_render(arguments.resolution, arguments.samples)

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

    manifest_path = os.path.join(arguments.output_dir, "manifest.json")
    with open(manifest_path, "w", encoding="utf-8") as manifest_file:
        json.dump(manifest, manifest_file, indent=2)
        manifest_file.write("\n")
    print("FORMLOOM_BLENDER_CUBE_RENDER_COUNT=" + str(len(manifest["renders"])))


if __name__ == "__main__":
    main()
