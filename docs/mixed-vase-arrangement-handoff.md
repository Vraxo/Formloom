# Phase 9 handoff — one tulip and one daisy in one ceramic vase

## Status and scope

Phase 9's fixed pair uses the existing tulip, daisy, vase, renderer, desktop prompt, and PNG-save
paths. PR [#52](https://github.com/Vraxo/Formloom/pull/52) was merged as
`1311f91e582554c2601c0b9476265a1a62ab123d`; post-merge Linux Release CI run
[42493951766](https://github.com/Vraxo/Formloom/actions/runs/42493951766) passed on that commit.
User review of the seed-42 output first identified the stem discontinuity at the vase opening, then
confirmed that contact looked repaired while flagging a sharp downward turn at both interior ends.
That bend came from the straight below-base extension and was not intended. Follow-up PR
[#53](https://github.com/Vraxo/Formloom/pull/53) now extends each stem along the reverse base
tangent with a cubic, then clips it inside the near lip. Source commit `ed6c204` and the final
workflow-restored head `54f7f4a` passed Linux Release run
[42502940039](https://github.com/Vraxo/Formloom/actions/runs/42502940039), including build, full
test suite, and preview upload. A temporary diagnostic workflow step was used for one run to locate
a test probe below the near-lip occlusion and has since been removed; no permanent workflow change
remains. The newest artifact has not been visually inspected in this sandbox because its binary
host is outside the outbound allowlist, so aesthetic verification of the tangent correction remains
pending. Timing sidecar values have also not been read. Do not merge PR #53 without explicit user
approval.

- **Starting point:** Phase 9 branched from `main` at `863dd75`, after Phase 8 PR #51 merged as
  `c441a9a`.
- **Phase 8 prerequisite:** `.agents/todo-8.md` and `docs/daisy-vase-handoff.md` were checked in
  full. Phase 8 was complete; post-merge Linux Release run
  [42452733510](https://github.com/Vraxo/Formloom/actions/runs/42452733510) passed. Its visual and
  timing evidence remains unchanged.
- **Design recorded before implementation:** `.agents/todo-9.md` and the initial PR description
  recorded the typed pair, strict grammar, profile-relative left/right layout, separate layout
  stream, and two-stem mouth continuation before source changes.
- **Implementation history:** the original Phase 9 code/artifact head was `46ae3d8`; its
  documentation/checklist head was `67f3550`. PR #52 merged as `1311f91`. The first contact fix
  landed in `4119570` with StyleCop cleanup in `d604e54`; the tangent-matched continuation is in
  `ed6c204`, with the full-resolution probe adjustment in `1d03d40`. A temporary failure-reporting
  workflow step was used once to locate a probe below the lip and removed in `54f7f4a`; it is not
  part of the final workflow. Final workflow-restored CI run `42502940039` passed.

## Supported prompts

Exactly these four prompts are added:

```text
a red tulip and a white daisy in a blue ceramic vase on a wooden table
a red tulip and a white daisy in a cream ceramic vase on a wooden table
a yellow tulip and a white daisy in a blue ceramic vase on a wooden table
a yellow tulip and a white daisy in a cream ceramic vase on a wooden table
```

Grammar is exactly `<a red|a yellow> tulip and a white daisy in <a blue|a cream> ceramic vase on a wooden table`.
Existing case normalization, repeated-whitespace normalization and terminal punctuation remain
in effect. Tulip-first order, the `and` separator, `in`, exact flower descriptors, vase glaze and
terminal table phrase remain fixed. Unsupported order, quantities, plurals, extra adjectives or
objects, other colors/species, other vessels, and alternate relationship wording are rejected as a
whole; they cannot degrade to a standalone flower or empty vase.

The standalone tulip and standalone daisy prompts still use their existing typed paths. No
same-species pair, third flower, general bouquet, arbitrary placement, or additional container
relationship is supported.

## Core model and composition

`VaseTulipDaisy` holds the exact `CeramicInstance` already stored by the scene, a closed tulip
color, fixed `DaisyAppearance.WhiteWithGoldenDisk`, and the existing `TulipPlant` and `DaisyPlant`
models. The vase is the only `SceneEntry`; the two plants are typed vase contents rather than
neighbors. Validation requires exactly one vase and one of each plant, rejects missing/invalid
geometry and extra entries, and makes the pair mutually exclusive with `VaseFlower`, `VaseDaisy`,
`BowlContainment` and `CupContents`.

The species generators and their named streams are reused unchanged: the vase uses `vase.form`,
the tulip uses `tulip.stem`, `tulip.leaves`, `tulip.bloom`, and the daisy uses `daisy.stem`,
`daisy.leaves`, `daisy.petals`, `daisy.disk`. Only composition jitter consumes
`vase.tulip-daisy.layout`.

The authored layout is fixed: tulip left, daisy right; the existing daisy stem reaches higher,
so its radial head sits above the tulip's cupped head. Both stem bases derive from the same
`OpeningCenter` / `OpeningRadius`:

- Base offsets are `±(0.22 + bounded jitter) × OpeningRadius.X`; the jitter range is
  `±0.025 × variation`.
- Head targets are `±(1.0 + bounded jitter) × OpeningRadius.X`; the jitter range is
  `±0.08 × variation`.
- A progressive fan compensates for each source stem's existing top sway so the bases and head
  targets retain their left/right ordering.
- The fixed 0.82 profile factor narrows stem/leaf geometry horizontally and uniformly scales each
  bloom around its transformed stem top. Stem/leaf vertical coordinates remain unchanged. The
  initial all-axis transform moved leaves toward the rim; review of the existing validator's leaf
  clearance rules led to preserving the source vertical positions in the final transform.

No canvas-pixel placement constants, unconstrained optimizer, or user-selected layout is added.
Core geometry tests verify opening fit (including thickness), stem and bloom separation, leaf
attachment, margins, deterministic layout, and bounded behavior across seeds and variation.

## Rendering and desktop integration

`StillLifeRenderer` now paints the shared ceramic first, then draws each complete flower and stem
as one path over the far rim. This removes the old join between a flower clipped at the mouth and a
separate mouth-continuation stroke. The interior stem segment receives the cavity-depth gradient. A short cubic continuation below
each typed stem base follows the reverse base tangent, then is clipped through the opening inset
by the near lip, avoiding a sharp directional break without overpainting that lip. Standalone
tulip, standalone daisy, and the mixed pair use this shared path. The species materials and plant
geometry remain separate, the pair adds no tabletop shadow, and vase/shadow invariants remain
covered. New row-by-row pixel checks assert visibility from the far-rim contact through the
opening, disappearance beneath the near lip, and no visible stem pixels on the vase wall at
preview and full-resolution sizes.

The strict pair parser runs before standalone fallbacks. Desktop prompt help adds one concise pair
example. Four subject tokens are whitelisted in `PngSaveService`:

| Prompt combination | Subject token | Default save filename |
| --- | --- | --- |
| red / blue | `red-tulip-and-white-daisy-in-blue-vase` | `formloom-red-tulip-and-white-daisy-in-blue-vase-seed-42.png` |
| red / cream | `red-tulip-and-white-daisy-in-cream-vase` | `formloom-red-tulip-and-white-daisy-in-cream-vase-seed-42.png` |
| yellow / blue | `yellow-tulip-and-white-daisy-in-blue-vase` | `formloom-yellow-tulip-and-white-daisy-in-blue-vase-seed-42.png` |
| yellow / cream | `yellow-tulip-and-white-daisy-in-cream-vase` | `formloom-yellow-tulip-and-white-daisy-in-cream-vase-seed-42.png` |

Desktop session tests cover all four prompts through generation/save metadata, safe filename
selection, failed-prompt preview/metadata retention, and the component's prompt-help text.

## Verification

The sandbox has no .NET SDK, so no local SDK build or test run was possible. CI is the authoritative
verification. Final workflow-restored run [42502940039](https://github.com/Vraxo/Formloom/actions/runs/42502940039)
passed Restore, Release build with warnings as errors, the full solution test step, test-result
upload, and `procedural-preview` upload. It includes the tangent-matched continuation and pixel
checks on the PR head. Earlier tangent-probe checks exposed a test sample below the near lip; a
one-run diagnostic step surfaced the row, the probe was moved above the lip, and the temporary
workflow step was removed before the final green run. The post-fix artifact is available on the PR
run, but this sandbox cannot fetch its Azure-hosted binary.

Coverage includes:

- Core/parser: four color × glaze prompts, normalization and strict rejection, typed same-vase
  identity, exclusivity, specific invalid-half errors, safe bounds, flower counts and species
  geometry; 4 combinations × 7 seeds × 4 variation values; independent named streams and
  standalone geometry stability.
- Renderer: non-vacuous petal, white-petal, disk, four-leaf and both-stem pixels; row-by-row
  continuity from far-rim contact through the mouth; tangent-matched continuation (including a
  full-resolution centerline check) and near-lip disappearance; wall-leak and changed-pixel bounds;
  shadow equality, silhouettes, diagnostic/detail omission, deterministic PNGs, and supported sizes.
- Desktop: all combinations through the pipeline, preview/save metadata, rejection retention,
  prompt help, and the four safe save tokens.

The existing workflow remains one Linux Release job; no workflow step, job, package, runner, or
artifact pipeline is added by the final change. Both temporary test-diagnostic steps used while
diagnosing assertions have been removed. Test-result and procedural-preview artifacts continue to
use the repository's existing upload steps.

## Review artifacts

The existing `procedural-preview` upload contains the generated files under the test assembly's
`artifacts/bin/Formloom.Tests/**`; no PNG is committed as source. The pair exporter keeps a compact
review sheet, its manifest, and the ten curated full-size cases by default:

- `formloom-vase-tulip-daisy-sheet.png` — 1280 × 960, four columns × three rows of 320 × 320 cells.
- `formloom-vase-tulip-daisy-sheet.timing.json` — authoritative row-major case map, prompt,
  colors, daisy appearance, glaze, seed, variation, filename, dimensions and per-cell timing.
- `formloom-vase-tulip-daisy-{name}-seed-{seed}-variation-{variation}.png` — 1024 × 1024 full-size
  sample, with matching `-silhouette.png` and `.timing.json` sidecars.

The ten row-major cells are:

| Cell | Case | Seed | Variation |
| ---: | --- | ---: | ---: |
| 0 | red / blue | 42 | 0.35 |
| 1 | red / cream | 42 | 0.35 |
| 2 | yellow / blue | 42 | 0.35 |
| 3 | yellow / cream | 42 | 0.35 |
| 4 | red / blue, alternate seed | 1337 | 0.35 |
| 5 | yellow / cream, alternate seed | 20261105 | 0.35 |
| 6 | red / blue, low variation | 42 | 0 |
| 7 | red / cream, high variation | 42 | 1 |
| 8 | yellow / blue, low variation | 20261105 | 0 |
| 9 | yellow / cream, high variation | 1337 | 1 |

A 320 px pixel-readability assertion runs for every sheet cell. The full-size pixel matrix runs for
all four default prompt combinations, and separate dimension coverage uses all combinations at
64 × 64, 320 × 320, 640 × 480, 2048 × 1024 and 4096 × 1024. `FORMFLOOM_EXPORT_ALL=1` continues to
control exhaustive exports for other families; it does not gate render tests.

**Post-fix visual review is pending.** The user-supplied seed-42 reproduction and enlarged rim crop
were visually inspected and confirm that the original stems broke at the vase contact. Inspect the
new `procedural-preview` artifact on the green [PR #53 CI run 42500331364](https://github.com/Vraxo/Formloom/actions/runs/42500331364) at normal sheet size and in
its ten full-resolution samples before marking the correction aesthetically verified. This agent
could not download the artifact because GitHub redirects it to a blob host outside the sandbox's
allowlist. Check both stems at contact and through the opening, near-lip occlusion, the four default
color/glaze combinations, leaves, flower separation, and variation-endpoint silhouettes.

## Timing and limitations

Each full-size case sidecar records render-plus-PNG-encode time plus framework, operating system,
processor count and PNG byte counts. The canonical red/blue case also measures the same empty vase,
standalone red tulip and standalone white daisy on the same runner; these are single observations,
not a benchmark or universal cost model. The sidecars are uploaded in `procedural-preview`, but
those numeric values have not been read into this handoff, so no timing numbers or performance
claim are made here. They remain a review follow-up alongside visual inspection of the artifact.

Headless Linux CI does not test Photino/WebView2 native dialogs, Windows window lifecycle, visual
accessibility, or native resize behavior. No such coverage is claimed. Phase 9's post-merge CI is
green, and follow-up PR #53's current Linux Release CI is green. Remaining gates are post-fix human
visual review, optional timing-sidecar read-back, explicit user approval before merging PR #53, and
its post-merge Linux Release run. Do not merge to close this handoff without the user's approval.
