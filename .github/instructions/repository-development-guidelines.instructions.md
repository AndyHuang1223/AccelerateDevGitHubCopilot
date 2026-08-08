---
name: repository-development-guidelines
description: Repository-wide C# and .NET development conventions
applyTo: "**"
---

# Repository Development Guidelines

This is a .NET 8 C# library management console application.

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
