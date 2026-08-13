---
name: conventional-commit
description: Suggest Conventional Commit messages from staged changes without committing.
argument-hint: "[optional focus]"
user-invocable: true
disable-model-invocation: true
---

# Conventional Commit

Use this skill when a person wants a commit-message suggestion from staged changes.

1. Read [commit conventions](./references/commit-conventions.md).
2. Run the platform-appropriate script in `./scripts` to collect status and staged diff.
3. Treat script output as objective facts; do not infer unstaged or unobserved changes.
4. Classify type, scope, and mixed concerns from the observed diff.
5. Return one or more suggested messages and explain any split between feature, test, and docs concerns.

Never run `git commit`, stage files, edit files, or push. If there is no staged diff, report that a message cannot be reliably suggested.
