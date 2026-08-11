# Library Management Repository Guidelines

## How to answer questions

- Read relevant source files, tests, and docs before answering; never guess the repository structure.
- Cite actual file paths, types, methods, tests, or observed command output as evidence.
- Separate observed facts, reasonable inferences, and unverified information.
- If the user asks only for explanation or analysis, do not modify files.
- For a plan, list acceptance criteria, affected areas, expected files, risks, and unresolved decisions first.
- Never invent commands, tests, tool calls, or documentation sources.

## Architecture and development

- Business rules belong in Library.ApplicationCore.
- Library.Infrastructure handles JSON persistence and repositories.
- Library.Console handles input, output, and flow coordination only.
- Follow existing entities, services, interfaces, enums, repositories, and factories.
- Prefer the smallest focused change; do not add packages or change public interfaces without a concrete requirement.
- Do not change seed data or unrelated behavior.

## Testing and verification

- Follow existing xUnit and NSubstitute conventions.
- Cover success, rejection, boundary, and no-side-effect paths.
- Before reporting completion, run and observe dotnet build, dotnet test, and git diff --check.
- Never claim a command succeeded unless its output was actually observed.
