# ADR-0001: Keep Formloom a hybrid procedural engine

- **Status:** Accepted
- **Date:** 2026-11-09
- **Scope:** Phase 11 model-system architecture

## Context

Formloom is a procedural image generator. Its strongest capability is constructing geometry and imagery through algorithms implemented in C#. Moving model knowledge into external files can improve extensibility, but that benefit does not justify replacing a clear procedural engine with a universal declarative language.

There are three concerns that must remain distinct:

1. **Algorithms and reusable capabilities:** geometry construction, curve mathematics, profile mapping, repetition, attachment, seeded variation, material evaluation, and rendering.
2. **Model knowledge:** contours, proportions, part counts, attachment locations, ordering, parameter ranges, and appearance variants.
3. **Serialization and authoring format:** the way model knowledge is stored, discovered, validated, and edited (initially JSON).

Treating all three as data would merely relocate procedural complexity into a less expressive format. Conversely, putting every model-specific contour and count into C# makes supported models unnecessarily expensive to add and maintain.

## Decision

Formloom will remain a **hybrid procedural engine**.

- C# owns reusable algorithms and mathematical capabilities. These implementations should be general, typed, testable, deterministic, and independently useful.
- External model data owns declarative facts and composition choices: profiles, counts, parameters, part ordering, attachment targets, material tokens, and bounded variation settings.
- JSON is a serialization and authoring format, **not** a programming language and not the place to reimplement general algorithms.
- A model that fits the existing engine vocabulary should be addable as external data without model-specific production code or an application rebuild.
- A capability should be added to the shared engine only when it expresses a real, model-independent requirement. Do not create abstractions merely to make a model fit JSON.
- A genuinely specialized algorithm may remain code-authored when expressing it through existing data operations would be less clear, more fragile, or substantially more complex. Do not disguise a species-specific function behind a falsely generic operation name.
- Not every future Formloom model is required to be data-authored. The data-only extension goal applies to models that fit the supported vocabulary; the engine may deliberately support specialized code paths when justified.
- Keep the existing implementation as the behavioral reference during migration. No legacy construction, validation, or rendering path is removed until the replacement has equivalent automated coverage and reviewed visual evidence.

## Consequences

### Positive

- Procedural mathematics remains in a language suited to algorithms.
- Model-specific data can evolve without duplicating construction code or requiring an application rebuild.
- The model format remains finite and comprehensible rather than growing into a general-purpose scripting language.
- Specialized models are not forced into awkward data graphs merely to satisfy an ideological purity test.

### Trade-offs

- The system may support both data-authored models and carefully justified code-authored models.
- The boundary between reusable engine capability and specialized model behavior requires design review.
- A data-only second-model proof remains necessary to demonstrate that the shared vocabulary is genuinely reusable, rather than a thin wrapper around the rose.

## Required review questions

Before adding a model-system operation or moving logic into model data, answer:

1. Is this a reusable algorithm, or is it model-specific information?
2. Would a normal C# implementation be clearer than expressing this as data?
3. Does the proposed operation have a model-independent contract and plausible reuse?
4. Does the external definition describe choices and composition, or is it becoming an imperative program?
5. Does the change preserve the legacy behavior until tests and visual review establish equivalence?

When in doubt, prefer the clearest maintainable implementation over maximizing the percentage of JSON.
