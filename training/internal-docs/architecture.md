# Engineering Architecture

- `Library.ApplicationCore` owns entities, contracts, result statuses, and business rules.
- `Library.Infrastructure` owns JSON loading, repositories, and persistence mechanics.
- `Library.Console` owns input/output and flow coordination.
- Entry points such as a future `Library.Api` must reuse ApplicationCore services instead of duplicating rules.
- Controllers must not access repositories directly or decide borrowing eligibility.

