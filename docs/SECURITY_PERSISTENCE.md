# Security Persistence Rules

RunBase uses EF Core with LINQ as the default persistence strategy.

Rules:

- Use LINQ for database queries whenever possible.
- Do not use `FromSqlRaw`, `ExecuteSqlRaw`, or `SqlQueryRaw`.
- If manual SQL is ever unavoidable, use interpolated/parameterized APIs only.
- Never build SQL by concatenating user input.
- Keep `DbContext` inside `RunBase.Infrastructure`.
- Application and API layers must not compose SQL queries.

This keeps the persistence path aligned with the V4 goal of reducing SQL Injection risk.
