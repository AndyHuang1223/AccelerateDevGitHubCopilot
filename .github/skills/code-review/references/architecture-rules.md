# Architecture rules

- ApplicationCore owns business rules and result statuses.
- Infrastructure owns JSON persistence and repository implementation.
- Console owns user interaction and flow coordination.
- An API transport must reuse ApplicationCore; controllers must not decide domain eligibility or access repositories directly.
