---
name: pr-readiness
description: Check a .NET change against build, tests, diff hygiene, and team Definition of Done.
argument-hint: "[optional review focus]"
user-invocable: true
disable-model-invocation: true
---

# PR Readiness

Use this skill as a quality gate after implementation and before review.

1. Read [Definition of Done](./references/definition-of-done.md) and [PR checklist](./references/pr-checklist.md).
2. Run the platform-appropriate verification script in `./scripts`.
3. Inspect the diff and map every acceptance criterion to evidence.
4. Check for tests for new behavior, regression, boundary, and no-side-effect paths.
5. Check public API, packages, seed data, configuration, migrations, and documentation.
6. Output Risk, evidence, blocking findings, and a final READY or NOT READY verdict.

A failed command, missing evidence, or unresolved blocking finding means NOT READY. Never commit, push, merge, deploy, edit files, or claim a result that was not observed.

