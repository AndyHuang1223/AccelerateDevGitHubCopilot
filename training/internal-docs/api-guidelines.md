# Company API Guidelines

- Use `/api/{resource}` names and explicit route constraints.
- Return RFC 7807 ProblemDetails for client-visible errors.
- Include an `X-Correlation-Id` response header for every request.
- Public endpoints require rate limiting and documented limits.
- Controllers are transport adapters; business decisions belong in ApplicationCore.
- Register dependencies through DI and keep DTOs separate from domain entities.
- Document public endpoints with OpenAPI and include health checks.

