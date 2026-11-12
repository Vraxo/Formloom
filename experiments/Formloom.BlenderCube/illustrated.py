"""Legacy illustrated still-life art-direction comparison."""

import time

from geometry import assign_material, create_grounded_apple, create_lathed_vase
from materials import create_material
from scene import clear_scene, create_illustrated_backdrop, create_illustrated_tabletop, create_lighting, render_view


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
