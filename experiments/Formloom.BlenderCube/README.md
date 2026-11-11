# Blender cube rendering experiment

A deliberately isolated proof of concept for evaluating Blender as a procedural rendering backend. It does not change the desktop application's rendering path or add a Blender dependency to Formloom Core.

## What it tests

- A cube mesh authored explicitly from eight vertices and six faces (not Blender's built-in cube primitive).
- A five-segment bevel modifier and weighted corner normals.
- Four independently assigned materials: matte clay, glazed ceramic, metallic, and procedural wood.
- Three camera positions using the same geometry: three-quarter, elevated, and low.
- A shared three-light studio setup, world fill, ground plane, and Cycles rendering.
- A geometry gallery with an explicitly authored cube, parametric UV sphere, and parametric cylinder.
- An authored apple mesh with a smoothed rotational profile, five-lobed shoulders and base, a recessed stem well, and a curved tapered stem.
- Three dedicated apple views plus a still-life composition reusing the same fruit mesh.
- A C#/.NET launcher that invokes Blender as a separate background process.

The output includes twelve cube material/view combinations, three geometry-gallery views, three apple-shape study views, and one still-life composition, plus a JSON manifest recording the render settings.

## Requirements

- .NET 10 SDK.
- Blender 3.6 or newer, available as `blender` on PATH (or pass `--blender` with its executable path).

No Blender installation, Python package, or Blender assembly is required by the .NET project itself.

## Run

From the repository root:

```sh
dotnet run --project experiments/Formloom.BlenderCube -- --output-dir artifacts/blender-cube
```

On Windows, if Blender is not on PATH:

```powershell
dotnet run --project experiments/Formloom.BlenderCube -- --blender "C:\Program Files\Blender Foundation\Blender 4.5\blender.exe" --output-dir artifacts\blender-cube
```

Optional settings:

```sh
dotnet run --project experiments/Formloom.BlenderCube -- --resolution 768 --samples 64 --output-dir artifacts/blender-cube
```

`--samples` accepts any integer from **1 to 256**; it is not a choice between fixed quality presets. The default is 32. Lower values make quick, noisy previews, while higher values progressively reduce Monte Carlo noise at the cost of render time. The CI run deliberately stays at 12 samples to keep its review artifact fast. Resolution is configurable from 64 to 2048 pixels.

The prototype deliberately uses Cycles on the CPU so the experiment runs on machines without a supported GPU. GPU configuration and performance comparison are deferred until the basic scene construction and visual output have been reviewed.

## Inspecting the scene

The Python scene script is intentionally readable and self-contained. It constructs geometry, materials, lights, and cameras directly through Blender's Python API. To inspect or modify the scene interactively, run the script from Blender's UI or add a `.blend` save step locally; the current automated output is the rendered PNG set and manifest.

## Scope boundary

This is an experiment, not a production renderer contract. It does not introduce a universal scene schema, integrate with prompt parsing, migrate any existing 2D renderer, or change the application's output path. Those decisions should follow from the results of this prototype.
