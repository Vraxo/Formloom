# Repository instructions for coding agents

Before changing this repository, read [CONTRIBUTING.md](CONTRIBUTING.md) and follow its incremental-delivery workflow.

The essential rule is: **make one small, independently verifiable improvement at a time; open and complete a focused PR; verify its merge on `main`; then choose the next step from what the merged result demonstrates.** Do not turn a roadmap phase into a large implementation batch, and do not accumulate unrelated work in one PR.

Use the latest merged `main` as the base for new work. Keep commits coherent, inspect the diff after every commit, run focused verification, and be explicit about checks that are pending or failing. Keep existing working behavior until a replacement has been demonstrated.

For model-system changes, also read [the model-system contract](docs/model-system.md) and [ADR-0001](docs/adr/0001-hybrid-procedural-engine.md). Reusable algorithms belong in C#; model-specific facts and composition belong in external model data. Do not introduce model-ID-specific production branches or turn the definition format into a general-purpose scripting language.

The **ChatGPT-only progress-update section** in `CONTRIBUTING.md` applies only to work performed through ChatGPT's interface. Agents operating elsewhere should ignore that presentation requirement while still following the shared incremental PR and verification workflow.
## Pull-request auto-merge

- Prefer enabling GitHub auto-merge for agent-authored PRs after opening them, so GitHub merges automatically once every required check passes.
- Before enabling it, verify that the repository ruleset requires **all relevant CI jobs** for the PR's changed area. For the Blender experiment, both `Linux / Release` and `Blender / Procedural geometry renders` must be required; a successful optional render job is not a merge safeguard.
- If any relevant workflow job is not a required status check, do not enable auto-merge for that PR. Report the gap and request or make the repository ruleset change through an authorized path first.
- After an auto-merge, verify the merge commit on `main` and its post-merge CI result before starting dependent work.
