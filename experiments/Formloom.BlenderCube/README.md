# Blender cube rendering experiment

A deliberately isolated proof of concept for evaluating Blender as a procedural rendering backend. It is the current 3D-first research path described in [ADR-0003](../../docs/adr/0003-explore-a-3d-first-rendering-path.md). It does not change the desktop application's rendering path, make Blender a production dependency, or add a Blender dependency to Formloom Core.

## What it tests

- A cube mesh authored explicitly from eight vertices and six faces (not Blender's built-in cube primitive).
- A five-segment bevel modifier and weighted corner normals.
- Four independently assigned materials: matte clay, glazed ceramic, metallic, and procedural wood.
- Three camera positions using the same geometry: three-quarter, elevated, and low.
- A three-light studio setup, world fill, 360-degree curved cyclorama, and Cycles rendering.
- Adjustable Cycles denoising using OpenImageDenoise.
- An apple quality study comparing 12, 32, 64, and 128 raw samples; when denoising is enabled, it also includes a 64-sample denoised comparison. Per-render times are included in the manifest.
- A geometry gallery with an explicitly authored cube, parametric UV sphere, and parametric cylinder.
- An authored apple mesh with a smoothed rotational profile, broad rounded lower body, shallow blossom dimple, and five-lobed shoulders/base, a recessed stem well, and a curved tapered stem.
- Three dedicated apple views plus a still-life composition reusing the same fruit mesh.
- A C#/.NET launcher that invokes Blender as a separate background process.

With denoising enabled (as in CI), the output includes twelve cube material/view combinations, three geometry-gallery views, three apple-shape study views, five apple quality comparisons, and one still-life composition (24 PNGs total). With denoising disabled, the denoised comparison is omitted (23 PNGs). Both configurations include a JSON manifest recording render settings and quality-study timings.

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
dotnet run --project experiments/Formloom.BlenderCube -- --resolution 768 --samples 64 --denoise true --output-dir artifacts/blender-cube
```

`--samples` accepts any integer from **1 to 256**; it is not a choice between fixed quality presets. The default is 32. Lower values make quick, noisy previews, while higher values progressively reduce Monte Carlo noise at the cost of render time. The CI run deliberately stays at 12 samples to keep its review artifact fast. Resolution is configurable from 64 to 2048 pixels. `--denoise true` enables OpenImageDenoise; it is off by default so the experiment remains usable with Blender builds that omit the denoiser. CI uses the official Blender 4.5.16 distribution, which includes the CPU denoiser.

The prototype deliberately uses Cycles on the CPU so the experiment runs on machines without a supported GPU. The quality study uses one fixed apple/camera/light setup so sample count and denoising are the only variables in that comparison. GPU configuration and performance comparison are deferred until the basic scene construction and visual output have been reviewed.

## Inspecting the scene

The Python scene script is intentionally readable and self-contained. It constructs geometry, materials, lights, and cameras directly through Blender's Python API. To inspect or modify the scene interactively, run the script from Blender's UI or add a `.blend` save step locally; the current automated output is the rendered PNG set and manifest.

## Scope boundary

This experiment informs the 3D-first development direction, but it is not a production renderer contract. It does not introduce a universal scene schema, integrate with prompt parsing, or change the application's output path. The current Skia renderer remains intact and is not being migrated or removed at this stage. Scene/backend contracts and any production integration must follow from reviewed results across representative objects and compositions.
