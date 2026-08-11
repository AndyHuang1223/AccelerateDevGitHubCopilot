# Repository and Copilot Guidelines

This is a .NET 8 C# library management console application.

## How to answer repository questions

- Read the relevant source files, tests, and documentation before answering; do not guess the repository structure.
- Cite the actual file paths, types, methods, tests, or command output used as evidence.
- Separate observed facts, reasonable inferences, and information that has not been verified.
- When the user asks only for explanation or analysis, do not modify files.
- When the user asks for a plan, first state acceptance criteria, affected areas, expected files, risks, and unresolved decisions.
- Never invent test results, command output, tool calls, or documentation sources.

## Architecture

- Business rules belong in Library.ApplicationCore.
- Library.Console handles input, output, and flow coordination only.
- Library.Infrastructure handles JSON persistence and repository implementation.
- Do not calculate business rules in the Console or Infrastructure layers.

## Development

- Follow existing entities, services, interfaces, enums, and repository patterns.
- Prefer the smallest focused change.
- Do not add a NuGet package unless the requirement clearly needs it.
- Do not change public interfaces without a concrete requirement.
- Do not change seed data or unrelated behavior.
- Preserve nullable reference type correctness.

## Testing and verification

- Follow the existing xUnit and NSubstitute test style.
- Cover success, rejection, boundary, and no-side-effect paths.
- Before reporting completion, run dotnet build, dotnet test, and git diff --check.
- Never claim a command succeeded unless its output was actually observed.
