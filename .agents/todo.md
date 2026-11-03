# Formloom — Initial Build Plan

> **Checkpoint status (2026-11-02):** Initial slice implemented; Linux Release CI passes through PR #10; Windows use and generated outputs are user-confirmed. Visual polish has been reviewed. This checklist now distinguishes delivered code/test evidence from individually unverified manual cases. See [milestone handoff](../docs/initial-milestone-handoff.md). No broader roadmap is authorized by this cleanup.

> **Checkpoint:** Stop after completing this plan and report back. Do not continue into the later roadmap without review.
>
> **First deliverable:** A runnable Formloom desktop application where a user can enter a simple prompt, generate a polished procedural still-life image of an apple on a wooden table, vary it with a seed and variation control, and save the result as a PNG.

## Project intent

Formloom is a CPU-first, non-neural, text-to-image image-construction engine. It creates images from explicit algorithms, geometry, object definitions, procedural materials, composition rules and rendering code. Its code and authored data should remain understandable and inspectable.

The first milestone is deliberately narrow. It must prove the complete path from text prompt to an actual image in the desktop application. It is not a demo made from a pre-existing image, a static mock-up, or a collection of unrelated graphics experiments.

### Definition of done

The first milestone is complete when all of the following are true:

- Formloom builds and launches as a native desktop window on Windows.
- The window contains a prompt field prefilled with `a red apple on a wooden table`, a seed control, a variation control, a Generate button, an image preview and a Save PNG action.
- Generating the default prompt creates a recognisable, attractive illustrated still life at 1024 × 1024 (or another clearly documented default resolution if there is a compelling technical reason).
- The image is constructed at runtime. No stock image, embedded finished image, downloaded texture, neural model, or other pre-rendered artwork is used.
- The apple has an organic, non-perfectly-circular silhouette, a stem and leaf, visible volume, highlights and shadows, and restrained surface detail.
- The wooden table has visible directional grain and some natural variation. The scene has a background, coherent lighting and a cast/contact shadow so the apple does not look like a flat sticker.
- Repeating a generation with the same prompt, seed, variation, resolution and application build produces the same rendered pixels on the same runtime/platform.
- Changing the seed produces a visible but coherent variation. Changing the variation control changes the amount of allowed variation without destroying the subject.
- The UI reports unsupported prompts and generation errors clearly instead of silently ignoring text or crashing.
- Save PNG produces a valid image file that can be opened outside Formloom.
- `dotnet build` and `dotnet test` both pass.

## Chosen technology

Use the following stack unless a package incompatibility is demonstrated by an actual build or runtime test:

- **Language/runtime:** C# 14 and .NET 10 (`net10.0`). Use the latest installed .NET 10 SDK servicing release.
- **Desktop host/UI:** Blazor hosted by **PhotinoX.Blazor 5.3.5**. This is the independently maintained Photino fork, not the original `Photino.Blazor` package. Verify the package/runtime setup rather than substituting a different UI framework pre-emptively.
- **Raster renderer:** **SkiaSharp 4.154.0**, using CPU raster surfaces for this milestone.
- **Math:** `System.Numerics` plus small, project-owned helpers where needed.
- **Tests:** **xUnit v3 (`xunit.v3` 4.0.2)**.
- **Data/configuration:** C# types for the first scene definitions; `System.Text.Json` only where serialization is actually needed.
- **Randomness:** A small project-owned seeded PRNG (PCG32 is a suitable choice), with stable derived streams for independent scene components.

Keep package versions centrally managed in `Directory.Packages.props`. Do not add a package merely because it might be useful later. If a listed version is unavailable or incompatible at implementation time, select the latest compatible stable release, record the reason in the README, and keep the change limited to that dependency.

## Working rules for the implementation agent

1. Read this file fully before coding. Execute the tasks in order and keep the solution buildable throughout.
2. Inspect the existing repository before creating or replacing files. Preserve existing work and conventions where possible; do not overwrite unrelated files.
3. Do not expand the scope beyond the checkpoint. In particular, do not start a universal scene grammar, multiple animal/object libraries, 3D rendering, a plugin system, an LLM integration, GPU rendering, or an elaborate editor.
4. Do not implement a large architecture speculatively. Add only the abstractions needed by the working slice, but keep the core scene description independent of SkiaSharp and the UI.
5. Use readable C# with nullable reference types enabled. Prefer one class or primary type per file, small focused helpers, explicit names and spaces rather than tabs. Avoid private nested classes. Comments should be limited to XML documentation where useful, TODOs, and explanations of genuinely non-obvious/critical behavior.
6. Represent expected user/input failures as ordinary result/error values; do not use exceptions for routine unsupported prompts or validation failures. Exceptions may still be appropriate for unexpected programming or I/O failures, but handle them at the UI boundary.
7. Do not use `object.GetHashCode()` to derive seeds. Its output is not a suitable cross-process seed contract. Use a stable, explicitly implemented hash for named random streams.
8. Keep generated visual content procedural. Do not use ImageGen, stock imagery, downloaded textures, embedded screenshots, or a hand-painted finished still-life image as the output.
9. Do not make the first renderer GPU-dependent. The app must work on an ordinary CPU-only machine.
10. Update the checkboxes in this file as tasks are completed. Do not mark a task complete unless its acceptance criteria are met.
11. Do not commit changes unless explicitly asked. Do not proceed beyond the final checkpoint below.

---

# Phase 0 — Establish the solution

### 0.1 Inspect the repository and environment

- [x] Inspect the repository root, current solution/projects, existing README, build configuration and Git status before changing anything.
- [x] Check `dotnet --info`, installed SDKs and the current target OS. Confirm that a .NET 10 SDK is available. If not, report the exact prerequisite rather than silently retargeting the project.
- [x] Check whether the Windows WebView2 Runtime is available. Photino uses WebView2 on Windows; document the prerequisite if it is absent.

**Done when:** The implementation plan accounts for the existing repository and the actual build environment. Existing user files are preserved.

> Inspection (2026-11-02): Debian 12 / Linux x86-64; `dotnet --info` fails with `dotnet: command not found`. Local SDK installation is not required for this session: per user direction, use Linux/.NET 10 Release GitHub Actions CI (adapted from `Vraxo/Lattice`), for restore/build/test validation. CI results must be checked before claiming validation. Windows WebView2 is not applicable on this host; the Windows prerequisite is documented in README. Retain the existing `.slnx` solution format rather than adding a duplicate `.sln`. The CI SDK and Release build/test path are now confirmed; no local SDK was installed.

### 0.2 Create or adapt the solution structure

- [x] Create or adapt `Formloom.sln`.
- [x] Establish this initial structure (adjust only if the repository already has a sound equivalent):

  ```text
  Formloom/
  ├── Formloom.slnx
  ├── Directory.Build.props
  ├── Directory.Packages.props
  ├── README.md
  ├── .agents/todo.md
  ├── Formloom.Core/
  ├── Formloom.Rendering.Skia/
  ├── Formloom.Desktop/
  └── Formloom.Tests/
  ```

- [x] Target all projects at `net10.0`. Enable nullable reference types and implicit usings. Use consistent formatting and sensible compiler warnings; do not suppress warnings wholesale.
- [x] Use Central Package Management in `Directory.Packages.props` so package versions are declared once.
- [x] Add the projects to the solution and establish only these references:
  - `Formloom.Rendering.Skia` references `Formloom.Core`.
  - `Formloom.Desktop` references `Formloom.Core` and `Formloom.Rendering.Skia`.
  - `Formloom.Tests` references `Formloom.Core` and `Formloom.Rendering.Skia`.
  - `Formloom.Core` must not reference SkiaSharp, Photino, Blazor or desktop/UI assemblies.
- [x] Add the selected package dependencies to the appropriate projects: PhotinoX.Blazor to Desktop, SkiaSharp to Rendering.Skia, and xUnit v3 to Tests.
- [x] Restore and build the empty solution. Create the test project early and verify that `dotnet test` discovers and executes a trivial passing test.

**Done when:** The solution restores, builds and runs its test project. Core remains independent of rendering and UI packages.

> Layout update: per user preference, all four projects live directly at the repository root; no `src/` or `tests/` wrapper directories.

> Scaffold update: retained `Formloom.slnx`; added the four prescribed projects and central package versions. Desktop is now an executable PhotinoX.Blazor host. A Core dependency smoke test and native Microsoft.Testing.Platform runner with TRX reporting are in place. Restore/build/test and test discovery passed Linux Release CI; no local SDK was installed.

### 0.3 Add a minimal developer README

- [x] Document the project purpose, the no-ML/CPU-first constraint, the project layout, required .NET SDK and the Windows WebView2 prerequisite.
- [x] Add the exact commands to restore, build, test and run the desktop app.
- [x] Keep the README factual and short; do not document future features as if they already exist.

---

# Phase 1 — Build the smallest useful generation core

> Implementation added: immutable scene types, bounded prompt grammar, PCG32 with FNV-1a named streams and normalized scene construction. Core tests passed Linux Release CI in PR #4 (27 seconds). No renderer or UI changes are included.

### 1.1 Define the first scene model

- [x] Add a small set of typed, immutable scene/request types in `Formloom.Core`. They should describe the prompt-derived still life, its canvas dimensions, seed, variation amount and relevant object parameters.
- [x] Keep the model semantic and renderer-independent. It may describe an apple, leaf, stem, tabletop, background and light direction, but must not contain `SKCanvas`, `SKPath`, `SKPaint`, `SKBitmap` or other Skia types.
- [x] Validate dimensions and variation bounds at the boundary. Use explicit, understandable validation results.
- [x] Avoid building a universal scene graph or a generalized material framework for one still-life scene.

**Done when:** Core can describe the first still-life scene without knowing how it will be drawn.

### 1.2 Implement deterministic randomness

- [x] Implement a small seeded PRNG in Core. PCG32 is preferred, but a comparably small, documented generator is acceptable.
- [x] Provide the minimal operations the generator needs, such as integer ranges, normalized floating-point values and probability checks.
- [x] Support deterministic derived streams using stable string/identifier hashing. Give independently generated components their own streams (for example `apple.shape`, `apple.surface`, `table.grain`, `background.detail`).
- [x] Ensure a change in one component's random consumption does not shift the random sequence of unrelated components.
- [x] Keep the core generator independent of `System.Random` for image-generation decisions. A system RNG may be used by the UI solely to choose a new seed when the user presses Randomize.
- [x] Add tests for repeatability, range boundaries, distinct seeds and independent named streams.

**Done when:** The same seed and stream name always produce the same sequence, and named streams remain isolated from one another.

### 1.3 Implement the initial prompt grammar

- [x] Implement a deliberately small, documented grammar for the first supported prompt family. At minimum, accept `a red apple on a wooden table` case-insensitively and tolerate harmless whitespace and terminal punctuation.
- [x] Support a small number of meaningful variations without pretending to understand arbitrary English. For example, allow a few apple colour adjectives such as red, green and yellow while retaining the apple-on-wooden-table construction.
- [x] Parse the prompt into the typed scene/request model. Keep parsing separate from rendering.
- [x] Reject unsupported subjects, materials or relationships with a friendly, specific message. Do not silently discard unknown words and generate an unrelated image.
- [x] Add tests for valid prompts, case/whitespace handling, supported modifiers, empty input and unsupported input.

**Done when:** The initial prompt family is predictable, testable and clearly bounded. No LLM or external NLP package is introduced.

### 1.4 Build the still-life scene description

- [x] Convert a successfully parsed request into a complete scene description, including object proportions, layout, light direction, background and table placement.
- [x] Use relative coordinates or a normalized layout so the scene can be rendered at more than one resolution without rewriting its composition.
- [x] Apply the seed to procedural details and the variation setting to the permitted range of shape/material irregularity. A higher variation value should create more diversity, not random structural damage.
- [x] Keep the apple recognisable at all variation values. Its major parts and their relationships must remain intact.
- [x] Add tests proving that scene construction is deterministic for identical inputs and that relevant input changes affect the scene description.

**Done when:** Core can produce a fully specified, reproducible still-life scene from a supported prompt, seed and variation setting.

---

# Phase 2 — Render an actual procedural image

> Renderer implementation added: CPU raster surfaces, runtime path geometry, gradients, directional wood grain, apple/stem/leaf, seeded surface detail and cast/contact shadows. The renderer returns dimensions plus PNG bytes and owns its native resources. Linux native assets and invariant/repeatability tests are included. Core/renderer tests passed CI, including the opacity, contact-shadow and grain regressions through PR #10. User-supplied 1024 × 1024 PNGs were visually reviewed. Multi-size rendering is tested automatically; visual inspection at other resolutions remains open.

### 2.1 Establish the Skia rendering boundary

- [x] Implement a small renderer API in `Formloom.Rendering.Skia` that accepts the Core scene description and returns a clearly defined rendered-image result (dimensions plus PNG bytes, or an equivalent clean representation).
- [x] Use SkiaSharp 4.x CPU raster surfaces for the first implementation. Do not add a GPU backend or expose Skia types outside the rendering project.
- [x] Ensure all surfaces, canvases, paints, paths and other native-backed Skia objects have clear ownership and are disposed correctly.
- [x] Add a minimal test proving that a scene can be rendered and encoded as a PNG.

**Done when:** The renderer can turn the Core scene description into a valid PNG without any UI involvement.

### 2.2 Render the composition and table

- [x] Render a restrained background with a visually intentional palette and a clear separation between background and tabletop.
- [x] Render the tabletop as a perspective-aware or convincingly angled surface rather than an unrelated flat rectangle.
- [x] Generate directional wood grain using procedural paths, layered strokes or another inspectable method. Vary the grain scale and strength; avoid uniform parallel stripes.
- [x] Add a small amount of seeded wood variation, such as knots or subtle colour changes. Keep the result controlled and believable.
- [x] Ensure the tabletop and background establish a clear surface on which the apple can rest.

**Done when:** The scene already reads as a coherent still-life setting before the apple is added.

### 2.3 Construct the apple from geometry

- [x] Draw the apple with custom organic path geometry. It must not be a perfect circle, ellipse or generic emoji-like icon.
- [x] Introduce controlled asymmetry and an identifiable apple profile, including the top indentation and lower lobes.
- [x] Add a stem and at least one leaf as independently constructed shapes with sensible attachment points and orientation.
- [x] Ensure the apple silhouette, stem and leaf vary within bounded limits according to their named random streams.
- [x] Keep the geometry readable and editable in code. Avoid a giant opaque path literal with no understandable construction logic.

**Done when:** The main subject is immediately recognisable as an apple, including its stem and leaf.

### 2.4 Add volume, materials and surface detail

- [x] Give the apple visible volume through layered gradients, directional shading and highlights. The light direction must agree with the cast/contact shadow.
- [x] Add restrained colour variation and a few subtle surface details (for example, speckles, tiny marks or bloom). They should enhance the surface rather than resemble random noise pasted over it.
- [x] Give the leaf a distinct green material treatment, midrib and restrained vein/detail work if appropriate.
- [x] Use a coherent wood material treatment for the table. Do not confuse geometric detail with material appearance.
- [x] Keep the illustration stylistically consistent. The target is a polished, attractive illustrated still life, not photorealism and not a set of flat primitives.

**Done when:** The apple and table have visually distinct materials and convincing depth cues.

### 2.5 Add lighting, contact and final composition

- [x] Add a soft cast/contact shadow that anchors the apple to the table.
- [x] Add any subtle ambient shading needed to make the scene feel integrated. Avoid excessive blur, bloom or decorative effects.
- [x] Check object scale, margins, focal placement and negative space at the target resolution.
- [x] Verify render/decode dimensions at default, smaller/larger and rectangular sizes, plus unchanged normalized composition (passing renderer/Core tests). Visual comparison at non-default sizes is deferred under D1.
  - [x] Automated render/decode dimensions at multiple sizes and normalized scene geometry are covered by passing tests.
  > **Deferred — D1: non-default-size and controlled low/high visual review.** This original manual clause was not individually executed; see the acceptance scope below.
- [x] Inspect the actual PNG visually. Iterate on obvious defects such as tangencies, awkward overlaps, flat shading, distracting grain or inconsistent light direction.

**Done when:** The complete result looks intentional and finished enough to be a useful first generator output, not merely a technical proof that drawing calls work.

### 2.6 Verify PNG output and renderer determinism

- [x] Encode the raster surface as a standards-compliant PNG and return its bytes with the actual dimensions.
- [x] Add tests for PNG signature, non-zero dimensions, expected dimensions and successful decoding.
- [x] Add a repeatability test showing identical inputs produce identical rendered pixels within the same build/runtime.
- [x] Add a test showing a different seed changes at least some visual detail while preserving valid output.
- [x] Avoid brittle golden-image tests that depend on hand-maintained binary snapshots at this stage. Test invariants and repeatability instead.

**Done when:** The renderer has automated coverage for valid output, repeatability and seed-driven variation.

---

# Phase 3 — Put the generator inside Formloom Desktop

> Current desktop wiring: prompt, uint64 seed, Randomize, variation, background generation, preview and native asynchronous Save PNG are implemented. A failed request retains the latest successful image and its seed/variation metadata. Expected validation and unexpected generation/save failures are presented without stack traces; unexpected details are logged. Save uses actual PNG bytes and the successful image's seed. No new packages were added.
>
> User confirmed the preceding host's interaction check on Windows (`Blazor interaction confirmed (4)`). The user subsequently confirmed the integrated Windows app works and supplied generated PNGs at several seeds. Specific edge-case, accessibility and close/relaunch checks remain individually unverified; general success is not evidence for every manual case. See README for exact checks.

### 3.1 Create the PhotinoX Blazor host

> Host implementation added using the upstream 5.3.5 startup API and sample: STA entry point, 1400 × 900 Formloom window, root component, copied static content and disposable application lifetime. The original interaction check was confirmed by the user and has since been replaced by the working generation UI. Host compilation passed CI and Windows launch is user-confirmed; clean shutdown/relaunch remains individually unverified.
>
> Renderer review status: PR #5 passed Linux CI; attempted to download its `procedural-preview` artifact, but the sandbox cannot access the redirected Azure artifact host (EOF). This limitation was later bypassed for visual review by user-uploaded PNGs; the CI artifact is still available outside this sandbox.

- [x] Create a minimal `Formloom.Desktop` PhotinoX.Blazor application using the package's current documented startup API.
- [x] Use PhotinoX.Blazor 5.3.5 and usable Windows runtime assets, evidenced by the central package and user-confirmed native Windows use. Linux native GUI launch is outside this acceptance scope (D2).
- [x] Configure the native window title as `Formloom` and choose a sensible initial desktop size (approximately 1400 × 900).
- [x] Register the Blazor root component and display a simple Formloom page.
- [x] Launch the native Windows app and use its Blazor page, as user-confirmed. Detailed lifecycle verification is deferred under D3, not claimed complete.

**Done when:** A working Formloom desktop window launches before the generator is wired into it.

### 3.2 Build the first usable UI shell

- [x] Create a clean, restrained dark desktop layout with Formloom branding. Prioritize the image preview over decorative chrome.
- [x] Include a prompt field, Generate button, numeric seed field, Randomize Seed action, variation slider, image-preview area and Save PNG action.
- [x] Prefill the prompt with `a red apple on a wooden table` and the seed with a fixed, reproducible value such as `42`.
- [x] Display the selected seed and variation value clearly. The user must be able to edit the seed directly.
- [x] Include a useful empty/initial preview state before the first generation.
> **Deferred — D3: native resizing/lifecycle verification; responsive CSS is implemented, not measured.** This original manual clause was not individually executed; see the acceptance scope below.

**Done when:** The UI exposes every control needed to exercise the first vertical slice, even before generation is connected.

### 3.3 Connect the UI to the generator

- [x] Add a small application-level generation service in Desktop that orchestrates prompt parsing, scene construction and Skia rendering.
- [x] Keep generation logic out of Razor components. Components should collect input, call the service and present its result.
- [x] On Generate, pass the prompt, seed, variation and target dimensions through the pipeline and display the returned PNG in the preview.
- [x] Show a clear, human-readable validation message for unsupported prompts. Keep the previous successful image visible when a later prompt fails, unless there is a compelling usability reason to clear it.
- [x] Disable or guard duplicate Generate actions while a generation is in progress. Show a simple generating state and restore the controls on success or failure.
- [x] Run CPU generation through `Task.Run` and preserve asynchronous session guards (source plus passing service/session tests). Native responsiveness/timing measurement is deferred under D4.
  - [x] Generation uses `Task.Run`; pending-work session tests pass.
  > **Deferred — D4: native timing/responsiveness measurement.** This original manual clause was not individually executed; see the acceptance scope below.
- [x] Make Randomize Seed choose a new seed, display it, and make it easy to generate that variation. It must not silently change the seed during a supposedly deterministic generation.

**Done when:** Clicking Generate in the desktop app produces and displays the image created by the actual Core + Skia pipeline.

### 3.4 Implement PNG saving

- [x] Implement Save PNG using the actual bytes from the latest successful generation, not a screenshot of the preview UI.
- [x] Use a reliable desktop/WebView-compatible download or save mechanism. If a small JS helper is required for a download link/blob, keep it isolated and documented.
- [x] Use a sensible default filename such as `formloom-apple-seed-42.png` and sanitize any user-derived filename components.
- [x] Disable Save PNG until a successful image exists.
- [x] Verify filesystem PNG bytes and decode/dimensions with passing writer/renderer tests and the user-supplied outputs. Independent desktop-viewer and native-dialog cases are deferred under D5.
  - [x] Real filesystem write and PNG decoding/dimensions are tested; uploaded outputs are 1024 × 1024.
  > **Deferred — D5: independent desktop viewer and native-dialog roundtrip.** This original manual clause was not individually executed; see the acceptance scope below.

**Done when:** A user can save a generated image as an actual PNG file.

### 3.5 Finish the first interaction details

- [x] Verify edited prompt/seed/variation snapshots in desktop session tests (PR #13). Actual rendered event-binding tests passed in PR #14.
- [x] Verify repeated session Generate calls use the real service/renderer and reproduce PNG output (PR #13). Rendered form-event tests passed in PR #14.
- [x] Verify Randomize updates session seed, retains the prior preview until Generate, and produces changed output through the real pipeline (PR #13). Rendered button/input tests passed in PR #14.
- [x] Verify variation scales bounded scene parameters and renders supported colors at maximum variation (passing Core/renderer tests). Specific low/high visual comparison is deferred under D1.
- [x] Provide concise, actionable UI errors for parser failures, rendering failures and save failures. Do not expose stack traces in normal UI messages; log unexpected technical details for development.
> **Deferred — D4: native keyboard/focus/contrast/accessibility review. Label/role markup assertions are included in the pending component tests, not a full accessibility audit.** This original manual clause was not individually executed; see the acceptance scope below.

**Done when:** The complete prompt → image → variation → save loop is pleasant and reliable to use.

---

> Visual defect reported by user: apple appeared translucent. The renderer reused shadow paint alpha (105/255) for the body gradient. Paint opacity is now explicitly reset before the body and highlight; a regression test varies the setting and checks that interior apple pixels do not change. PR #8 passed CI and the user confirmed the opacity fix; uploaded PNGs show the opaque apple.

> User confirmed the opacity fix works and supplied the actual seed-42 PNG for review. The agreed next polish is limited to contact and stem attachment: tighter lower-lobe contact shadows, a softer cast shadow, a fading stem indentation and a tapered shaded stem. Wood grain and body highlight are deliberately unchanged for a focused comparison. PR #9 passed CI. The subsequent user-uploaded PNG was reviewed: grounding and stem attachment improved. Seeds differed, so this was not a controlled same-seed comparison.

> Next user-approved visual pass: table grain only. Replaced the dense parallel strokes and concentric ring knot with broader low-contrast tonal bands, sparser perspective-scaled fibers and a shared flow field that bends around a shaded, seeded knot. Apple geometry, lighting and contact shadows remain unchanged. Added table-region repeatability/seed coverage. PR #10 passed CI. The user supplied seed 4003733692769532287: the calmer grain and overall scene were visually reviewed as suitable for this checkpoint. A controlled same-seed comparison was not performed.

# Phase 4 — Final verification and checkpoint

> Verification update (2026-11-02): PR #7 merged after Linux / Release CI passed (36 seconds), covering restore, warnings-as-errors Release compilation of the full solution and Core/renderer tests. The user subsequently confirmed the integrated app works on Windows. This is not evidence that every individual manual case below was exercised. Debug was later verified by the user on Windows; it is intentionally not duplicated in PR CI. Visual review has since been performed on user-supplied output through PR #10. Stop at this checkpoint; remaining unchecked verification is listed in the handoff.


### 4.1 Automated verification

- [x] Run `dotnet restore` from the solution root.
- [x] Run `dotnet build` for the complete solution in Debug and Release configurations (Release: PR #14 CI; Debug: user-reported Windows build, all five projects).
  - [x] Release builds the full solution with warnings as errors in CI.
  - [x] Debug build succeeded on Windows in 10.5 seconds using warnings as errors and a command-scoped source override; no duplicate Debug PR job is added.
- [x] Run `dotnet test` and ensure all Core and renderer tests pass.
- [x] Resolve warnings and errors introduced by the implementation. Do not hide them by disabling analyzers or suppressing broad warning categories.
> **Deferred — D3: exact clean-build command provenance on Windows; general Windows launch is confirmed.** This original manual clause was not individually executed; see the acceptance scope below.

### 4.2 Manual acceptance test

- [x] Launch Formloom Desktop.
- [x] Verify default prompt, seed 42, variation 35% and 1024 output through the real desktop pipeline (PR #13), in addition to user-supplied seed-42 output.
- [x] Inspect the image at normal viewing size and confirm it is recognisably an apple still life with coherent lighting, organic shape, visible materials and a cast/contact shadow.
- [x] Verify identical repeated Generate calls through the real session/service/renderer pipeline (PR #13).
- [x] Change the seed and confirm a visibly different but coherent result.
- [x] Verify controlled variation bounds/deviation in Core tests and valid rendering at maximum variation. Visual low/high comparison is deferred under D1.
- [x] Verify a real unsupported prompt yields an error and retains the previous image/metadata through the desktop pipeline (PR #13). Rendered alert tests passed in PR #14.
- [x] Verify actual PNG file export and decoding outside the component, plus externally supplied 1024 PNGs (passing filesystem/renderer tests). Independent desktop-viewer exercise is deferred under D5.
> **Deferred — D3: individual close/relaunch lifecycle check.** This original manual clause was not individually executed; see the acceptance scope below.

### 4.3 Documentation and agent handoff

- [x] Update README with the implemented feature set, exact prerequisites, build/test/run commands and any known platform limitations.
- [x] Ensure this `todo.md` accurately reflects completed tasks. Do not mark future roadmap features as complete.
- [x] Record the exact package versions used and any deviations from the chosen stack.
- [x] Provide a concise completion report containing: project structure, key implementation decisions, build/test commands and results, where the generated PNG can be found or saved, and any remaining defects or limitations.
- [x] Review actual generated output images supplied by the user. A full application screenshot is explicitly deferred under D6.
  - [x] User-provided generated PNGs were visually reviewed.
  > **Deferred — D6: full running-application screenshot.** This original manual clause was not individually executed; see the acceptance scope below.

## STOP HERE — CHECK IN BEFORE CONTINUING

Once every applicable task above is complete, **stop and report back for review**. Do not continue by adding more objects, materials, a generalized scene editor, advanced grammar, more rendering backends or an AI language translator. The next priorities should be chosen after inspecting the first real output and the quality of the implementation.

---

## Explicitly out of scope for this checkpoint

These are future possibilities, not acceptance criteria for the first slice:

- A universal natural-language parser or a tiny language model.
- Multiple object families (animals, people, furniture, buildings, landscapes, etc.).
- A universal object/scene grammar or a sophisticated plugin architecture.
- 3D meshes, skeletal animation, physically based rendering or a full 3D renderer.
- GPU acceleration, CUDA, Vulkan or OpenGL.
- Advanced editing features, layer trees, inpainting, selection tools or generation history.
- Photorealism or parity with Stable Diffusion.
- Network access, model downloads, training or any machine-learning dependency.


## User-approved reliability follow-up (after initial checkpoint)

The user explicitly approved this limited follow-up after the initial handoff. It does not authorize new subjects, composition controls or grammar expansion.

- [x] Extract desktop interaction state into a session shared by Razor and headless tests; retain failed-image state and successful-image metadata and guard overlapping actions.
- [x] Separate native destination selection from PNG writing; stage writes in the destination directory and clean up failed replacement attempts.
- [x] Add a dedicated `Formloom.Desktop.Tests` project (Desktop reference plus friend-assembly access), leaving the original Core/renderer test references unchanged.
- [x] Author session, real generation-service and real filesystem tests, including failures and controlled pending tasks.
- [x] Document Windows cancellation, resizing, keyboard and close/relaunch checks in `docs/windows-smoke-test.md`.
- [x] Verify the new tests/build in Linux Release CI. PR #12 passed Linux / Release in 33 seconds, including both test projects. No new runner or package dependency was added; no local SDK installation.
- [x] Obtain user confirmation of the refactored Windows app and agree the routine verification gate. The user confirmed it works and explicitly accepted passing, appropriately scoped tests instead of repeated manual smoke checks. This supersedes requiring the full smoke checklist on every change; it does not establish execution of every individual manual case.

### Checkpoint verification policy and audit

The user confirmed the refactored application works and accepted appropriately scoped CI tests as the routine gate, not repeated manual smoke passes. That does not complete every original checkbox or prove GUI/native coverage. The previous blanket closure statement was premature. See [the evidence audit](../docs/checklist-audit.md) for every previously unchecked criterion, matching tests/source evidence, and outstanding gaps. Parent checkboxes stay open where only part of the original criterion is established.

### Audit follow-up tests

- [x] Author real session→service→renderer tests for default 1024 output, repeated generation, unsupported-prompt retention and Randomize→Generate.
- [x] Extend the pending-request test to capture edited prompt and preserve its metadata across subsequent edits.
- [x] Run the audit pipeline tests and prompt assertions in Linux Release CI: PR #13 passed in 50 seconds.

No new feature milestone is selected by this audit. Unverified component event wiring, native dialog behavior, resizing, lifecycle, accessibility, Debug build and specific visual comparisons remain explicit evidence gaps rather than silently completed or waived tasks.


## Agreed acceptance scope and final closure gates

The user approved rendered-component tests, a one-off Debug verification and explicit deferral of native/manual checks instead of repeated smoke-test requests. Earlier original manual clauses above are now labeled **Deferred**, not falsely checked as executed. Passing PR #13 is established evidence; the component tests subsequently passed in PR #14.

| Deferral | Original manual evidence not established | Agreed treatment |
| --- | --- | --- |
| D1 | Non-default-size / controlled same-seed and low/high variation visual comparisons | Automated dimensions, normalized geometry, variation bounds and pixel repeatability are the gate. Additional visual comparisons deferred. |
| D2 | Photino native runtime launch in the Linux sandbox | Windows is the user-confirmed target. Linux CI builds and runs headless tests; Linux GUI launch deferred. |
| D3 | Native resizing, close/relaunch, exact clean-build launch provenance | General Windows use is confirmed. These specific manual cases are deferred, not mandatory on every PR. |
| D4 | Native responsiveness timing, keyboard/focus/contrast/accessibility | CPU work is off-thread. Component label/role/state tests do not establish full accessibility or native performance. Manual checks deferred. |
| D5 | OS save-dialog behavior and independent desktop-viewer exercise | PNG writer/decoder/session tests are the routine gate. Native dialog cancellation/overwrite prompts are deferred. |
| D6 | Screenshot of the full running application | Generated PNGs were reviewed. Full application screenshot deferred. |

- [x] Add rendered App component tests for defaults/labels, input events, submission, alerts, retained previews, disabled controls, Randomize and save snapshot behavior.
- [x] Add a test-only bUnit dependency and inject the existing session into App; production services still create the same session delegates.
- [x] Pass rendered-component tests and the full Release build in CI: PR #14, run 41511913612, success in 46 seconds.
- [x] Provide an opt-in `verify_debug` workflow-dispatch input for a one-off Debug build in the same Linux job; ordinary PR CI remains Release-only.
- [x] Execute the one-off Debug build: the user reported all five projects succeeded on Windows in 10.5 seconds. Command: `dotnet build Formloom.slnx --configuration Debug --warnaserror -p:RestoreSources=https://api.nuget.org/v3/index.json`. This was local verification, not a dispatched run; dispatch was blocked by HTTP 403, so no Debug run URL is claimed.

Both execution gates above now pass. The milestone is complete **under the revised acceptance scope**, not under every original manual criterion. D1–D6 remain explicitly deferred. No additional feature scope is authorized here.
