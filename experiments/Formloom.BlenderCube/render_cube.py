"""CLI entry point for the Formloom Blender geometry and still-life experiment."""

import argparse
import json
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from geometry import (
    assign_material,
    create_cube,
    create_grounded_apple,
    create_lathed_vase,
    create_parametric_cylinder,
    create_parametric_sphere,
)
from illustrated import render_illustrated_still_life
from materials import create_material
from photorealistic import render_photorealistic_still_life, render_quality_variant
from scene import (
    clear_scene,
    configure_render,
    create_camera,
    create_ground,
    create_lighting,
    render_view,
)


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
        "comparison": "historical comparison; same object geometry, object placement, camera, resolution, sample count, and denoising",
        "samples": arguments.samples,
        "denoising": arguments.denoise,
    }

    photorealistic_render = render_photorealistic_still_life(arguments.output_dir, arguments.resolution)
    manifest["renders"].append(photorealistic_render)
    manifest["photorealism_study"] = {
        "baseline": "still-life-vase-and-apple.png",
        "photorealistic": photorealistic_render["file"],
        "comparison": "same vase/apple geometry, object placement, camera, resolution, sample count, and denoising; physically based surface detail, natural oak, plaster, and photographic lighting differ",
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
