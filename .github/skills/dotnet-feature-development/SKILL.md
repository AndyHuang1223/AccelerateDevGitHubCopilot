---
name: dotnet-feature-development
description: >
  Implement or modify features in this C# .NET library management
  application while following its ApplicationCore, Infrastructure,
  Console, repository, testing, and verification conventions. Use when
  adding or changing services, business rules, repositories, Console
  flows, or related tests.
argument-hint: "[feature or change to implement]"
---

# .NET Feature Development

Use this workflow for changes that cross the Library ApplicationCore, Infrastructure, Console, or UnitTests projects.

## Workflow

1. Read the relevant code, tests, and documentation before proposing changes. Use [architecture guidance](./architecture.md) to identify the correct layer.
2. Convert the request into explicit acceptance criteria. Identify decisions that are not established by the repository or request; do not silently invent them.
3. Produce a small implementation plan with the affected files before editing.
4. Put business rules in ApplicationCore, persistence mechanics in Infrastructure, and input/output or flow coordination in Console.
5. Reuse existing entities, services, interfaces, enums, repository patterns, and test factories whenever possible.
6. Add tests for success, rejection, boundary, and no-side-effect paths. Follow [testing guidance](./testing.md).
7. Run dotnet build, dotnet test, and git diff --check before reporting completion. Report only commands and results that were actually observed.
8. If code and requirements conflict, stop and show the evidence, options, risks, and decision needed.

## Scope guardrails

- Prefer the smallest focused change.
- Do not add NuGet packages, database infrastructure, Web API layers, or unrelated refactors.
- Do not change seed data or public interfaces without a concrete requirement.
- Do not claim tests or commands passed without their output.
