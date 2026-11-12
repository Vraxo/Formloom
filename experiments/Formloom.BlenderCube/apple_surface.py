"""Procedural apple-skin maps built from an irregular surface point process.

The feature placement follows the general point-process approach used in
procedural texture synthesis research (including PPTBF), but this is a small,
self-contained implementation rather than a port of semiproctex or DiffPPTBF.
All maps are generated deterministically in Blender at render time.
"""

import math

import bpy
import numpy as np


MAP_RESOLUTION = 1536
LENTICEL_TARGET = 1450
LENTICEL_MINIMUM_SPACING = 0.028
IMAGE_NAMES = {
    "color": "FormloomAppleSkin_BaseColor",
    "roughness": "FormloomAppleSkin_Roughness",
    "height": "FormloomAppleSkin_Height",
}


def _smoothstep(value):
    value = np.clip(value, 0.0, 1.0)
    return value * value * (3.0 - 2.0 * value)


def _periodic_value_noise(width, height, cells_x, cells_y, rng):
    """Bilinear value noise periodic around the fruit, bounded at both poles."""
    grid = rng.random((cells_y + 1, cells_x)).astype(np.float32)
    x = np.arange(width, dtype=np.float32) * (cells_x / width)
    y = np.arange(height, dtype=np.float32) * (cells_y / height)
    x0 = np.floor(x).astype(np.int32)
    y0 = np.floor(y).astype(np.int32)
    tx = _smoothstep(x - x0)
    ty = _smoothstep(y - y0)
    x1 = (x0 + 1) % cells_x
    y1 = np.minimum(y0 + 1, cells_y)

    lower_left = grid[y0[:, None], x0[None, :]]
    lower_right = grid[y0[:, None], x1[None, :]]
    upper_left = grid[y1[:, None], x0[None, :]]
    upper_right = grid[y1[:, None], x1[None, :]]
    lower = lower_left * (1.0 - tx[None, :]) + lower_right * tx[None, :]
    upper = upper_left * (1.0 - tx[None, :]) + upper_right * tx[None, :]
    return (lower * (1.0 - ty[:, None]) + upper * ty[:, None]).astype(np.float32)


def _triangle_data(obj):
    mesh = obj.data
    uv_layer = mesh.uv_layers.active
    if uv_layer is None:
        raise RuntimeError("Apple mesh is missing its skin UV layer.")

    mesh.calc_loop_triangles()
    triangle_positions = []
    triangle_uvs = []
    areas = []

    for triangle in mesh.loop_triangles:
        positions = [mesh.vertices[index].co.copy() for index in triangle.vertices]
        area = 0.5 * (positions[1] - positions[0]).cross(
            positions[2] - positions[0]
        ).length
        if area <= 1e-10:
            continue

        uvs = [uv_layer.data[index].uv.copy() for index in triangle.loops]
        if not 0.035 < sum(uv.y for uv in uvs) / 3.0 < 0.965:
            continue

        triangle_positions.append(positions)
        triangle_uvs.append(uvs)
        areas.append(area)

    if not areas:
        raise RuntimeError("Apple UV layer contains no usable surface triangles.")

    return triangle_positions, triangle_uvs, np.asarray(areas, dtype=np.float64)


def _sample_density(field, u, v):
    height, width = field.shape
    x = int(u * width) % width
    y = max(0, min(height - 1, int(v * height)))
    return float(field[y, x])


def _sample_lenticels(obj, density_field, rng):
    """Sample non-overlapping marks over the actual mesh surface area."""
    positions, uvs, areas = _triangle_data(obj)
    cumulative_area = np.cumsum(areas)
    total_area = float(cumulative_area[-1])
    minimum_spacing = LENTICEL_MINIMUM_SPACING
    cell_size = minimum_spacing
    minimum_spacing_squared = minimum_spacing * minimum_spacing
    spatial_hash = {}
    points = []
    maximum_attempts = LENTICEL_TARGET * 42
    attempts = 0

    while len(points) < LENTICEL_TARGET and attempts < maximum_attempts:
        attempts += 1
        triangle_index = int(np.searchsorted(
            cumulative_area,
            rng.random() * total_area,
            side="right",
        ))
        triangle_index = min(triangle_index, len(positions) - 1)

        root = math.sqrt(rng.random())
        second = rng.random()
        weights = (1.0 - root, root * (1.0 - second), root * second)

        triangle_positions = positions[triangle_index]
        triangle_uvs = uvs[triangle_index]
        point = (
            triangle_positions[0] * weights[0]
            + triangle_positions[1] * weights[1]
            + triangle_positions[2] * weights[2]
        )
        uv = (
            triangle_uvs[0] * weights[0]
            + triangle_uvs[1] * weights[1]
            + triangle_uvs[2] * weights[2]
        )
        u = float(uv.x) % 1.0
        v = float(uv.y)
        if not 0.04 < v < 0.96:
            continue

        # A slowly varying density field makes some regions naturally richer
        # in lenticels without allowing marks to collapse into random clusters.
        density = 0.52 + 0.48 * _sample_density(density_field, u, v)
        if rng.random() > density:
            continue

        key = (
            math.floor(point.x / cell_size),
            math.floor(point.y / cell_size),
            math.floor(point.z / cell_size),
        )
        separated = True
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    neighbor_key = (key[0] + dx, key[1] + dy, key[2] + dz)
                    for neighbor in spatial_hash.get(neighbor_key, ()):
                        if (point - neighbor).length_squared < minimum_spacing_squared:
                            separated = False
                            break
                    if not separated:
                        break
                if not separated:
                    break
            if not separated:
                break
        if not separated:
            continue

        spatial_hash.setdefault(key, []).append(point.copy())
        radius = float(np.clip(
            rng.lognormal(mean=math.log(0.0028), sigma=0.31),
            0.00125,
            0.0048,
        ))
        strength = float(rng.uniform(0.35, 0.82))
        aspect = float(rng.uniform(0.72, 1.32))
        angle = float(rng.uniform(0.0, math.tau))
        local_radius = math.hypot(point.x, point.y)
        points.append((u, v, radius, strength, aspect, angle, local_radius))

    if len(points) < int(LENTICEL_TARGET * 0.8):
        raise RuntimeError(
            "Apple lenticel sampling stalled: generated "
            + str(len(points))
            + " of "
            + str(LENTICEL_TARGET)
            + " target marks."
        )
    return points


def _stamp_lenticel(halo, core, height, roughness, point, meridian_length):
    width = halo.shape[1]
    image_height = halo.shape[0]
    u, v, radius, strength, aspect, angle, local_radius = point
    circumference = max(0.22, 2.0 * math.pi * local_radius)
    meridian_length = max(0.25, float(meridian_length))

    extent = radius * 2.25
    radius_x = max(2, int(math.ceil(extent * width / circumference)))
    radius_y = max(2, int(math.ceil(extent * image_height / meridian_length)))
    center_x = int(u * width) % width
    center_y = int(v * image_height)
    x_indices = (center_x + np.arange(-radius_x, radius_x + 1)) % width
    y_indices = center_y + np.arange(-radius_y, radius_y + 1)
    valid_y = (y_indices >= 0) & (y_indices < image_height)
    y_indices = y_indices[valid_y]
    if y_indices.size == 0:
        return

    delta_x_pixels = (
        (x_indices - center_x + width // 2) % width - width // 2
    ).astype(np.float32)
    delta_y_pixels = (y_indices - center_y).astype(np.float32)
    delta_x = delta_x_pixels[None, :] * (circumference / width)
    delta_y = delta_y_pixels[:, None] * (meridian_length / image_height)

    cosine = math.cos(angle)
    sine = math.sin(angle)
    rotated_x = cosine * delta_x + sine * delta_y
    rotated_y = -sine * delta_x + cosine * delta_y
    normalized_radius = np.sqrt(
        (rotated_x / (radius * 1.55 * aspect)) ** 2
        + (rotated_y / (radius * 1.55 / aspect)) ** 2
    )
    outer = strength * (1.0 - _smoothstep((normalized_radius - 0.65) / 1.25))
    center = strength * (1.0 - _smoothstep((normalized_radius - 0.08) / 0.68))
    region = np.ix_(y_indices, x_indices)
    halo[region] = np.maximum(halo[region], outer)
    core[region] = np.maximum(core[region], center)
    height[region] -= center * 0.105
    height[region] += outer * 0.012
    roughness[region] += center * 0.055 + outer * 0.012


def _image_from_rgb(name, rgb, resolution):
    image = bpy.data.images.get(name)
    if image is None or image.size[0] != resolution or image.size[1] != resolution:
        if image is not None:
            bpy.data.images.remove(image)
        image = bpy.data.images.new(
            name,
            width=resolution,
            height=resolution,
            alpha=False,
            float_buffer=True,
        )

    rgba = np.empty((resolution, resolution, 4), dtype=np.float32)
    if rgb.ndim == 2:
        rgba[:, :, 0] = rgb
        rgba[:, :, 1] = rgb
        rgba[:, :, 2] = rgb
    else:
        rgba[:, :, :3] = rgb
    rgba[:, :, 3] = 1.0
    rgba = np.ascontiguousarray(rgba, dtype=np.float32)
    source_min = float(rgb.min())
    source_max = float(rgb.max())
    source_mean = float(rgb.mean())
    print(
        "APPLE_ARRAY_DIAGNOSTIC="
        + name
        + " min=" + str(source_min)
        + " max=" + str(source_max)
        + " mean=" + str(source_mean)
        + " first=" + str(tuple(float(value) for value in rgba[0, 0]))
        + " center=" + str(tuple(float(value) for value in rgba[resolution // 2, resolution // 2]))
    )
    image.pixels.foreach_set(rgba.reshape(-1))
    image.update()
    before_color_space = tuple(float(value) for value in image.pixels[0:4])
    print(
        "APPLE_IMAGE_WRITE_DIAGNOSTIC="
        + name
        + " after_set=" + str(before_color_space)
        + " size=" + str(tuple(image.size))
        + " source=" + str(image.source)
    )
    if max(source_max, 0.0) > 0.0 and max(before_color_space[:3]) <= 1e-8:
        raise RuntimeError("Blender image pixel write produced black pixels for " + name)
    image.colorspace_settings.name = (
        "sRGB" if name == IMAGE_NAMES["color"] else "Non-Color"
    )
    image.alpha_mode = "NONE"
    image.update()
    after_update = tuple(float(value) for value in image.pixels[0:4])
    print("APPLE_IMAGE_UPDATE_DIAGNOSTIC=" + name + " after_update=" + str(after_update))
    if max(source_max, 0.0) > 0.0 and max(after_update[:3]) <= 1e-8:
        raise RuntimeError("Blender image update cleared pixels for " + name)
    image.use_fake_user = True
    return image


def create_apple_surface_maps(obj, resolution=MAP_RESOLUTION):
    """Create deterministic color, roughness and relief maps for an apple mesh."""
    cached = [bpy.data.images.get(name) for name in IMAGE_NAMES.values()]
    if all(
        image is not None
        and image.size[0] == resolution
        and image.size[1] == resolution
        for image in cached
    ):
        cached_count = int(cached[0].get("lenticel_count", 0))
        obj["apple_lenticel_count"] = cached_count
        obj["apple_skin_map_resolution"] = resolution
        return {
            key: bpy.data.images[name]
            for key, name in IMAGE_NAMES.items()
        }

    if obj.data.uv_layers.active is None:
        raise RuntimeError("Apple mesh must have UV coordinates before material creation.")

    rng = np.random.default_rng(20261112)
    broad = _periodic_value_noise(resolution, resolution, 10, 8, rng)
    medium = _periodic_value_noise(resolution, resolution, 34, 26, rng)
    fine = _periodic_value_noise(resolution, resolution, 230, 180, rng)
    micro = _periodic_value_noise(resolution, resolution, 620, 470, rng)

    u = np.arange(resolution, dtype=np.float32)[None, :] / resolution
    warped_u = (
        u
        + (broad - 0.5) * 0.018
        + (medium - 0.5) * 0.007
    )
    vertical_striation = np.sin(
        math.tau * (
            warped_u * 17.0
            + (broad - 0.5) * 1.35
            + (medium - 0.5) * 0.58
        )
    )
    pigment = np.clip(
        0.76
        + (broad - 0.5) * 0.48
        + (medium - 0.5) * 0.19
        + vertical_striation * 0.075,
        0.28,
        0.985,
    )

    # The red blush sits over a warm, slightly golden under-colour. The
    # meridional streaks are broad and broken up, never a regular stripe mask.
    warm = np.asarray((0.405, 0.125, 0.026), dtype=np.float32)
    red = np.asarray((0.245, 0.012, 0.014), dtype=np.float32)
    color = warm[None, None, :] * (1.0 - pigment[:, :, None])
    color += red[None, None, :] * pigment[:, :, None]
    tonal_variation = (
        0.965
        + (broad - 0.5) * 0.105
        + (fine - 0.5) * 0.026
    )
    color *= tonal_variation[:, :, None]
    amber_undertone = np.clip((0.50 - pigment) * 0.90, 0.0, 0.20)
    color += amber_undertone[:, :, None] * np.asarray(
        (0.055, 0.032, 0.006),
        dtype=np.float32,
    )

    roughness = (
        0.418
        + (broad - 0.5) * 0.052
        + (medium - 0.5) * 0.038
        + (fine - 0.5) * 0.022
    ).astype(np.float32)
    height = (
        0.5
        + (medium - 0.5) * 0.042
        + (fine - 0.5) * 0.026
        + (micro - 0.5) * 0.016
    ).astype(np.float32)

    density_field = (
        broad * 0.58 + medium * 0.30 + fine * 0.12
    ).astype(np.float32)
    lenticels = _sample_lenticels(obj, density_field, rng)
    halo = np.zeros((resolution, resolution), dtype=np.float32)
    core = np.zeros((resolution, resolution), dtype=np.float32)

    meridian_length = float(obj.data.get("apple_uv_meridian_length", 2.0))
    for lenticel in lenticels:
        _stamp_lenticel(
            halo,
            core,
            height,
            roughness,
            lenticel,
            meridian_length,
        )

    # Lenticels are subdued brown-red pores with a soft warm margin, not
    # bright yellow dots. Their shallow pit and local roughness are separate.
    color += halo[:, :, None] * np.asarray(
        (0.018, 0.009, 0.0025),
        dtype=np.float32,
    )
    center_mix = np.clip(core * 0.64, 0.0, 0.64)[:, :, None]
    pore_color = np.asarray((0.105, 0.020, 0.009), dtype=np.float32)
    color = color * (1.0 - center_mix) + pore_color[None, None, :] * center_mix

    # A low-amplitude epidermal grain breaks the highlight at macro scale.
    height += (micro - 0.5) * 0.010
    roughness = np.clip(roughness, 0.34, 0.56)
    height = np.clip(height, 0.32, 0.68)
    color = np.clip(color, 0.0, 1.0)

    if not (
        np.isfinite(color).all()
        and np.isfinite(roughness).all()
        and np.isfinite(height).all()
    ):
        raise RuntimeError("Apple procedural maps contain non-finite values.")

    print(
        "APPLE_GENERATED_MAP_DIAGNOSTIC="
        + " color=" + str((float(color.min()), float(color.max()), float(color.mean())))
        + " roughness=" + str((float(roughness.min()), float(roughness.max()), float(roughness.mean())))
        + " height=" + str((float(height.min()), float(height.max()), float(height.mean())))
    )
    images = {
        "color": _image_from_rgb(IMAGE_NAMES["color"], color, resolution),
        "roughness": _image_from_rgb(IMAGE_NAMES["roughness"], roughness, resolution),
        "height": _image_from_rgb(IMAGE_NAMES["height"], height, resolution),
    }
    images["color"]["lenticel_count"] = len(lenticels)
    obj["apple_lenticel_count"] = len(lenticels)
    obj["apple_skin_map_resolution"] = resolution
    return images
