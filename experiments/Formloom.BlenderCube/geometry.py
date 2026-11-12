"""Procedural mesh and curve construction for experiment objects."""

import math

import bpy
from mathutils import Vector

from materials import principled_node


STUDIO_FLOOR_HEIGHT = -1.085
APPLE_LOWEST_PROFILE_HEIGHT = -0.64
APPLE_DEFAULT_SCALE = (0.82, 0.78, 0.92)


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
    # The apple mesh itself forms the shallow recessed stem well. Keep the
    # stem rooted inside that depression rather than adding a raised dark
    # sphere, which reads like a glossy button on the fruit.
    stem_curve = bpy.data.curves.new(name + "StemCurve", "CURVE")
    stem_curve.dimensions = "3D"
    stem_curve.resolution_u = 16
    stem_curve.bevel_depth = 0.016
    stem_curve.bevel_resolution = 3
    spline = stem_curve.splines.new("BEZIER")
    spline.bezier_points.add(2)
    stem_points = (
        (0.0, 0.0, 0.395 * scale[2]),
        (0.012 * scale[0], 0.004 * scale[1], 0.54 * scale[2]),
        (0.055 * scale[0], 0.016 * scale[1], 0.78 * scale[2]),
    )
    for point, coordinate, radius in zip(
        spline.bezier_points,
        stem_points,
        (0.95, 0.68, 0.32),
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
    stem_shader.inputs["Roughness"].default_value = 0.82
    stem.data.materials.append(stem_material)

def assign_apple_skin_uvs(mesh, sampled_profile, segments, scale):
    """Map the rotational apple surface with approximately uniform physical V scale."""
    radial_scale = 0.5 * (scale[0] + scale[1])
    cumulative = [0.0]
    for index in range(1, len(sampled_profile)):
        previous_height, previous_radius = sampled_profile[index - 1]
        height, radius = sampled_profile[index]
        vertical_distance = (height - previous_height) * scale[2]
        radial_distance = (radius - previous_radius) * radial_scale
        cumulative.append(cumulative[-1] + math.hypot(vertical_distance, radial_distance))

    total_length = max(cumulative[-1], 1e-6)
    v_coordinates = [distance / total_length for distance in cumulative]
    uv_layer = mesh.uv_layers.new(name="AppleSkinUV")
    for ring in range(len(sampled_profile) - 1):
        for segment in range(segments):
            polygon = mesh.polygons[ring * segments + segment]
            u0 = segment / segments
            u1 = (segment + 1) / segments
            coordinates = (
                (u0, v_coordinates[ring]),
                (u1, v_coordinates[ring]),
                (u1, v_coordinates[ring + 1]),
                (u0, v_coordinates[ring + 1]),
            )
            for loop_index, coordinate in zip(polygon.loop_indices, coordinates):
                uv_layer.data[loop_index].uv = coordinate

    mesh["apple_uv_meridian_length"] = total_length


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
        # The crown rolls inward into a real shallow stem well instead of
        # tapering to a pointed top. This keeps the shoulder full and soft.
        (0.55, 0.38), (0.54, 0.29), (0.50, 0.21),
        (0.43, 0.13), (0.405, 0.07), (0.395, 0.015),
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
                0.042 * five_lobes + 0.012 * subtle_irregularity
            ) * lobe_strength
            vertical_variation = 0.018 * five_lobes * lobe_strength
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
    assign_apple_skin_uvs(mesh, sampled_profile, segments, scale)
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
