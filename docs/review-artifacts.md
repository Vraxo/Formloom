# Focused CI review artifacts

A focused review artifact is a small, self-contained download that helps a reviewer answer a specific visual or evidence question without browsing a large generated-output archive. It is a convenience layer over CI evidence, not a substitute for automated tests or diagnostics.

## When to make one

- Add one when the normal CI artifact contains many unrelated files or is awkward to transfer, inspect, or review.
- Keep it optional and purposeful; do not create a new bundle for every build when the existing artifact is already easy to use.
- Do not reduce automated coverage or remove useful diagnostic outputs just to make the download smaller.

## What to include

- Start from the review questions. Include the compact comparison/summary view, the representative full-size examples needed to inspect detail, and the matching metadata or sidecars needed to interpret those examples.
- Include meaningful robustness cases when they are part of the review. Do not silently curate away inconvenient or failing cases.
- Add a brief `README` or index with the review order, file groups, dimensions or relevant settings, and any limits on what the observations establish.
- Keep names descriptive and stable. Prefer one clearly named CI artifact that downloads as a unit over a long list of individually selected files.
- Keep generated outputs in CI artifacts, not Git, unless a generated file is intentionally a maintained source asset.

## How to assemble it

- Select outputs deterministically using focused paths or names; do not glob an entire output tree into the focused bundle.
- Validate the expected files (or expected count and categories for a fixed set). Fail the job if required review evidence is missing rather than uploading a silently incomplete bundle.
- When the broad preview or diagnostics artifact remains useful, preserve it separately and add the focused bundle alongside it. Do not replace a useful artifact contract without a reason.
- Assemble and upload from the existing job that produces the files when practical. A separate workflow job, runner, package, or service needs an independent justification; bundle convenience alone is not a reason to add infrastructure.

## Verification and handoff

- Verify the assembly and upload steps in CI, then confirm the artifact name and contents in the run's artifact list. A passing build alone does not establish that the bundle was assembled or uploaded correctly.
- In the milestone handoff or checklist, link the run and artifact and summarize its contents. Point to the broad artifact too when it has complementary value.
- For visual or performance material, distinguish inspection and one-runner observations from automated guarantees, benchmarks, or broad performance claims.

## Example

Phase 10's `phase10-rose-review` bundle is the concrete example: two 2 × 2 review sheets, eight full-size renders, four silhouettes, and their ten timing sidecars, plus an index. The existing `procedural-preview` remains available for broader inspection. Both artifacts are assembled in the existing Linux / Release job, and the focused bundle guards the expected set before upload.
