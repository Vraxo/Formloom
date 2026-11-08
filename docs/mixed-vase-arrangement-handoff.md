# Phase 9 handoff — one tulip and one daisy in one ceramic vase

## Status and scope

Phase 9 implements a fixed pair using the existing tulip, daisy, vase, renderer, desktop prompt,
and PNG-save paths. PR [#52](https://github.com/Vraxo/Formloom/pull/52) is open on
`arena/7dff6cbd-formloom`. Do not merge it without explicit user approval. Linux Release CI run
[42493291154](https://github.com/Vraxo/Formloom/actions/runs/42493291154) passed on documentation
head `67f3550`; the implementation and curated-artifact commit `46ae3d8` also passed run
[42491100329](https://github.com/Vraxo/Formloom/actions/runs/42491100329). The live
[PR checks](https://github.com/Vraxo/Formloom/pull/52/checks) remain the source of truth for the
current head. Human visual review remains **pending**: no aesthetic pass is claimed from generated
output or pixel assertions alone.

- **Starting point:** Phase 9 branched from `main` at `863dd75`, after Phase 8 PR #51 merged as
  `c441a9a`.
- **Phase 8 prerequisite:** `.agents/todo-8.md` and `docs/daisy-vase-handoff.md` were checked in
  full. Phase 8 was complete; post-merge Linux Release run
  [42452733510](https://github.com/Vraxo/Formloom/actions/runs/42452733510) passed. Its visual and
  timing evidence remains unchanged.
- **Design recorded before implementation:** `.agents/todo-9.md` and the initial PR description
  recorded the typed pair, strict grammar, profile-relative left/right layout, separate layout
  stream, and two-stem mouth continuation before source changes.
- **Implementation history / ending code commit:** source implementation `1199641`; CI analyzer
  fixes `9475563`; the rim-clearance layout correction `4621b4f`; validation assertion fix
  `ab9041a`; curated review-artifact coverage `46ae3d8`. Commit `46ae3d8` is the last
  implementation/test/artifact commit; documentation/checklist commit `67f3550` also passed Linux
  Release CI. The README, this handoff and checklist are closure on the same open PR, whose live
  head is shown in [#52](https://github.com/Vraxo/Formloom/pull/52). A temporary test-annotation CI
  step used to expose the earlier assertion failure was reverted; the final workflow diff adds no
  step or job.

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

`StillLifeRenderer` materializes the existing species-specific content values, draws tulip then
daisy before the shared vase under the existing flower/mouth-top clip, and passes both stems into
the ceramic mouth continuation. The same opening/lip-inset mask keeps the stems in the cavity and
behind the near lip. The daisy remains rendered by `DaisyMaterial`, the tulip by `TulipMaterial`,
and each plant retains its green geometry/material path. The pair adds no tabletop shadow; shadow
mask, vase body and existing single-flower paths are covered by regression assertions.

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
verification. The current implementation run before the final artifact/diagnostic additions was
[42491100329](https://github.com/Vraxo/Formloom/actions/runs/42491100329), which passed Restore,
Release build with warnings as errors, the full solution test step, test-result upload, and
`procedural-preview` upload. The final docs/artifact commit still needs its own green run; record its
commit and URL here before treating the PR checks as final.

Coverage includes:

- Core/parser: four color × glaze prompts, normalization and strict rejection, typed same-vase
  identity, exclusivity, specific invalid-half errors, safe bounds, flower counts and species
  geometry; 4 combinations × 7 seeds × 4 variation values; independent named streams and
  standalone geometry stability.
- Renderer: non-vacuous petal, white-petal, disk, four-leaf and both-stem pixels; base fit,
  separated stems/blooms, mouth continuation and near-lip occlusion; changed-pixel bounds, shadow
  equality, silhouettes, diagnostic/detail omission, deterministic PNGs and supported square and
  rectangular sizes.
- Desktop: all combinations through the pipeline, preview/save metadata, rejection retention,
  prompt help, and the four safe save tokens.

The existing workflow remains one Linux Release job; no workflow step, job, package, runner, or
artifact pipeline is added by the final change. The interim test-failure annotation step used while
diagnosing CI is absent from the final tree. Test-result and procedural-preview artifacts continue
to use the repository's existing upload steps.

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

**Visual review is pending.** Open the `procedural-preview` artifact attached to the green
[PR #52 CI run](https://github.com/Vraxo/Formloom/actions/runs/42493291154) and inspect the sheet
at normal size and its ten full-resolution samples before marking the aesthetic gate passed. In
particular check the four default glaze/color combinations, both leaves per plant, flower
separation, natural entry through one mouth, lip occlusion, and variation-endpoint silhouettes.
This handoff does not claim the agent has visually inspected those images.

## Timing and limitations

Each full-size case sidecar records render-plus-PNG-encode time plus framework, operating system,
processor count and PNG byte counts. The canonical red/blue case also measures the same empty vase,
standalone red tulip and standalone white daisy on the same runner; these are single observations,
not a benchmark or universal cost model. The sidecars are uploaded in `procedural-preview`, but
those numeric values have not been read into this handoff, so no timing numbers or performance
claim are made here. They remain a review follow-up alongside visual inspection of the artifact.

Headless Linux CI does not test Photino/WebView2 native dialogs, Windows window lifecycle, visual
accessibility, or native resize behavior. No such coverage is claimed. The remaining acceptance
gates are human visual review, final commit CI, merge only with explicit approval, and post-merge
Linux Release CI; do not merge to close this handoff without the user's approval.
