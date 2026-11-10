# Canonical render baselines

**Status:** Initial user-captured baselines recorded  
**Baseline commit:** `996b54e3b771293918dabbd9a396894a4d9e6910`  
**Renderer:** Existing desktop prompt-to-PNG path (legacy scene generator and Skia CPU renderer)

These two images are the **before** snapshots for the first visual-improvement work. They document what the application produced before any rendering changes made under the accepted Naturalistic Still-Life Illustration direction.

They are not target/reference artwork. The target is defined separately in [the visual art direction](art-direction.md).

## Captured baseline set

Both images were generated with the same settings:

| Setting | Value |
|---|---:|
| Seed | `42` |
| Variation | `35%` |
| Output dimensions | `1024 × 1024` |
| Antialiasing | On |

The prompts below use the application's supported grammar verbatim.

| ID | Exact prompt | What it exercises |
|---|---|---|
| `apple-red-wooden-table` | `a red apple on a wooden table` | Organic fruit silhouette and volume, fruit-skin response, surface detail, tabletop, and contact/cast shadow |
| `rose-red-blue-vase-wooden-table` | `a red rose in a blue ceramic vase on a wooden table` | Layered petals, leaves and stem, glazed ceramic, shared lighting, object contact, and composition |

The machine-readable source of truth is [render-baselines.json](render-baselines.json).

## Image custody and integrity

The original PNGs were saved locally by the maintainer. They are intentionally **not committed to the repository**:

- The files are not available to CI or to repository reviewers.
- Their original local filenames and SHA-256 checksums have not been supplied to the repository.
- The manifest therefore records the capture definitions, not fabricated file hashes or a claim that CI has archived the images.

Keep the two saved originals unchanged. When comparing a rendering change, generate a new image with the exact prompt and settings above and compare it with the corresponding saved original. Do not overwrite the original baseline.

If the images are later added to a controlled review-artifact workflow, record their actual filenames and checksums at that time. Until then, this document plus the maintainer-held originals define the initial visual comparison set.

## How to use these baselines

1. Before a rendering change, regenerate both prompts with the recorded settings and retain the new outputs separately.
2. Compare the new apple against the apple baseline and the new rose against the rose baseline.
3. Inspect each at thumbnail size and at full resolution. Evaluate silhouette, volume, light direction, material response, texture scale, edge treatment, and composition using the questions in [the art-direction review criteria](art-direction.md#6-visual-review-and-acceptance).
4. Record intentional differences and any regressions. A visual change is not automatically an improvement merely because it is different.
5. Do not declare a shared lighting or material improvement based on the rose alone; the apple is the cross-material check.

The rose is the first development stress case. The apple is the first cross-check. A beverage-in-cup scene can be added as a later baseline when liquid and reflective-surface behavior becomes the subject of a focused change; it is not part of this initial two-image capture.

## What this baseline does and does not guarantee

- It fixes the user-visible prompt, seed, variation, output size, and antialiasing setting for repeatable manual comparisons.
- It does not guarantee byte-identical PNG output across runtime, Skia, or platform changes.
- It is not an automated pixel-golden test.
- It does not prove that CI has captured the two local images.
- It does not change the renderer, generation pipeline, prompt grammar, or visual output.
