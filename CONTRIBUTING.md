# Contributing and incremental delivery

Formloom is developed through **small, sequential, independently reviewable pull requests**. The goal is to make progress through a clear chain of verified improvements—not through large phases, sweeping rewrites, or mega-PRs that take a long time to understand and validate.

This is the default working agreement for maintainers, contributors, and coding agents.

## The working agreement

1. **Start from the latest merged `main`.** A change must build on the current, verified repository state. Do not base dependent work on stale snapshots or parallel branches.
2. **One coherent improvement per PR.** Each PR should solve one clearly stated problem or add one closely related capability. Keep its scope as small as practical.
3. **Split before work becomes a batch.** Do not combine unrelated architecture, refactoring, migration, UI, and cleanup work simply because they belong to the same broad roadmap item. When a change becomes too large or difficult to review, split it at a meaningful boundary and deliver the first useful slice.
4. **Make every step demonstrable.** A PR should include focused tests, fixtures, documentation, or other evidence that shows what it adds and how it builds on the existing code. Preserve previously working behavior unless the PR explicitly replaces it with verified equivalent behavior.
5. **Keep commits small and coherent.** Prefer one focused implementation commit per PR. When a correction or follow-up commit is necessary, it should address the specific issue found; each commit should leave the branch in a coherent, buildable state.
6. **Finish dependent work sequentially.** Complete, merge, and verify one PR before starting the next change that depends on it. Let what was learned in the merged result determine the next step.
7. **Treat roadmaps as direction, not batch instructions.** Milestones and long-term plans describe the destination. They are not a reason to implement many future steps at once or to keep a large phase open while unrelated work accumulates.

## The PR loop

For each improvement:

1. Inspect the latest `main`, the relevant implementation, its tests, and the governing architecture documents. Identify one concrete gap or opportunity.
2. State the PR's purpose, narrow scope, non-goals, and the evidence that will demonstrate success.
3. Create a branch from the latest `main`. Implement the smallest useful slice.
4. After each commit, inspect the resulting diff and run the most relevant focused build/tests. Keep the diff limited to the intended change, and confirm that already-merged work has not been accidentally removed or reverted.
5. Open a PR with a clear summary, rationale, scope boundary, and verification plan.
6. Run the full required CI checks. Do not describe a build or test as passed while it is still running, and do not claim visual parity without reviewing the relevant output.
7. If verification fails, inspect the actual failure and make the smallest targeted correction in its own commit. Re-run the relevant checks; do not continue piling on new features while the branch is broken.
8. Merge only when the PR is reviewable, mergeable, and its required checks have passed. Then verify the resulting `main` commit and post-merge CI before choosing the next improvement.

A useful PR should make it easy to answer: **What changed? Why is this the next step? What proves it works? What remains deliberately out of scope?**

## Verification and honesty

- Report exactly which build, tests, CI jobs, and visual checks were run, and whether each passed, failed, or is still running.
- A successful build is not the same as a successful test suite; a PR check is not the same as post-merge verification.
- When a check fails, use its logs to identify the cause. Avoid unrelated fixes in the same correction.
- For rendering changes, use the repository's established preview/review artifacts and inspect them when visual behavior is part of the acceptance criteria.
- Keep legacy implementations available until the replacement has sufficient structural, deterministic, and visual evidence. Do not delete a working path merely because a new data representation exists.

## ChatGPT-only progress updates

**This section applies only when the work is being carried out through ChatGPT's chat interface. Other coding agents and tools that cannot render ChatGPT progress UI should ignore this section.**

After **every commit**, ChatGPT should send a visible update before continuing:

- Render an actual progress bar in the chat UI (using the interface's supported visual components; do not merely paste HTML or code that looks like a progress bar).
- Show progress toward the current PR becoming ready to merge—not progress toward an entire roadmap or phase.
- Briefly state what the commit changed, its short SHA, what focused verification has actually completed, the current CI/PR status, and the next concrete step.
- Keep the percentage meaningful and explainable. It is a communication aid, not evidence that a build, test, review, or CI check passed.
- Report verification status separately from the bar. Never let a high percentage imply that checks are green when they are pending or failing.

The update is required after each commit, including small CI-fix commits. This makes the work visibly incremental and lets the maintainer see that the branch is progressing toward a reviewable PR rather than sitting idle or growing into an opaque batch.
