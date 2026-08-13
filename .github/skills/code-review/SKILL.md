---
name: code-review
description: Review a change for correctness, architecture, security, testing, maintainability, performance, and breaking changes.
argument-hint: "[optional review focus]"
user-invocable: true
disable-model-invocation: true
---

# Code Review

Use this skill for a structured, read-only review.

1. Read [review checklist](./references/review-checklist.md), [architecture rules](./references/architecture-rules.md), and [security checklist](./references/security-checklist.md).
2. Run the platform-appropriate context script in `./scripts` only to collect observable diff facts.
3. Understand the change intent and classify risk.
4. Review correctness, architecture, security, testing, maintainability, performance, and breaking changes.
5. Return actionable findings with severity, file/line evidence, criterion/rule, recommendation, and blocking status.
6. Mark command output that is unavailable as UNVERIFIED.

Do not edit files, stage or commit changes, run arbitrary commands, or claim unobserved results.

