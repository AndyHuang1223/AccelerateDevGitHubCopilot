# Library architecture reference

## Project responsibilities

| Project | Responsibility |
|---|---|
| Library.ApplicationCore | Entities, enums, service and repository contracts, and business rules |
| Library.Infrastructure | JSON loading, relationship population, repository implementation, and persistence |
| Library.Console | Console input/output, state transitions, dependency injection, and flow coordination |
| UnitTests | ApplicationCore and Infrastructure tests using xUnit and NSubstitute |

## Dependency direction

~~~text
Library.Console ───────> Library.Infrastructure ───────> Library.ApplicationCore
       └────────────────────────────────────────────────> Library.ApplicationCore

UnitTests ──────────────> Library.Infrastructure
UnitTests ──────────────> Library.ApplicationCore
~~~

Business decisions belong in ApplicationCore. Infrastructure may answer data queries and save data, but it must not decide whether a patron is eligible to borrow. Console should display service results and coordinate the interaction, not duplicate domain rules.
