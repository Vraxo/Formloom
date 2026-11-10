# Repository instructions for coding agents

Before changing this repository, read [CONTRIBUTING.md](CONTRIBUTING.md) and follow its incremental-delivery workflow.

The essential rule is: **make one small, independently verifiable improvement at a time; open and complete a focused PR; verify its merge on `main`; then choose the next step from what the merged result demonstrates.** Do not turn a roadmap phase into a large implementation batch, and do not accumulate unrelated work in one PR.

Use the latest merged `main` as the base for new work. Keep commits coherent, inspect the diff after every commit, run focused verification, and be explicit about checks that are pending or failing. Keep existing working behavior until a replacement has been demonstrated.

For model-system changes, also read [the model-system contract](docs/model-system.md) and [ADR-0001](docs/adr/0001-hybrid-procedural-engine.md). Reusable algorithms belong in C#; model-specific facts and composition belong in external model data. Do not introduce model-ID-specific production branches or turn the definition format into a general-purpose scripting language.

The **ChatGPT-only progress-update section** in `CONTRIBUTING.md` applies only to work performed through ChatGPT's interface. Agents operating elsewhere should ignore that presentation requirement while still following the shared incremental PR and verification workflow.
