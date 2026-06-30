# AGENTS.md

This file defines mandatory instructions for every AI coding assistant working in the OpenStore repository.

These rules are project requirements, not suggestions.

---

## 1. Required reading

Before changing code, read:

1. `.ai/CONTEXT_MAP.md`
2. `README.md`
3. `AGENTS.md`
4. `docs/PROJECT_STRUCTURE.md`
5. The task-relevant engineering docs from `docs/`
6. `.ai/README.md`
7. The applicable `.ai/skills/**/SKILL.md`
8. Related ADRs
9. Existing code and tests in the target service

Do not modify code before identifying the owning bounded context.

Use `.ai/CONTEXT_MAP.md` as a routing index to reduce unnecessary context loading. It is not a replacement for source files, ADRs, or exact implementation code.

These rules are portable across AI coding tools. For Codex, OpenCode, Antigravity CLI, or another agent, use `docs/AI_AGENT_PORTABILITY.md` and the tool-specific startup files when needed.

For code changes, follow `docs/GIT_WORKFLOW.md`: one feature branch per feature, with small coherent commits when committing is requested.

---

## 2. Approved architecture

- .NET 10
- ASP.NET Core Controllers (ControllerBase)
- Entity Framework Core
- PostgreSQL through Npgsql for runtime persistence
- Angular mobile-first PWA
- Simple API first
- Single API project during the MVP
- Feature folders inside the API
- One AppDbContext at the beginning
- Generic Repository plus Unit of Work
- YARP gateway
- RabbitMQ through the official .NET client only when asynchronous messaging is justified
- OpenTelemetry
- xUnit.net v3
- NSubstitute
- WireMock.Net

### 2.1 Authentication MVP state

- Production scheme: JWT Bearer (`AddJwtBearer()`).
- Token issuing (Identity, login, register, refresh) is deferred.
- `ICurrentUserContext` (scoped) resolves the `sub` claim from the bearer token.
- The `[Authorize]` attribute protects private endpoints.
- Current tests are unit tests. Controller tests instantiate controllers directly and substitute service dependencies; no test authentication handler is used unless a future task explicitly requests API integration tests.
- Postman requests will need a valid bearer token once Identity exists.

Do not introduce an alternative framework, ORM, database, broker, mapper, mediator, validation library, test framework, or mocking framework without an approved ADR.

---

## 3. Mandatory coding rules

### 3.1 Language

- Code, identifiers, comments, logs, exception types, test names, documentation, commits, and pull requests must be in English.
- User-facing localization is handled separately through resources.

### 3.2 One return statement

- Every non-void method must have exactly one return statement.
- Early returns are forbidden.
- Expression-bodied methods are allowed because they represent one result.
- `void` and `Task` methods must not use early `return` statements to change control flow.
- Validation must use helpers, typed exceptions, or an accumulated result instead of early returns.
- Do not use nested logic unnecessarily to satisfy this rule; extract cohesive private methods.

### 3.3 Null checks

Use:

```csharp
value is null
value is not null
```

Do not use:

```csharp
value == null
value != null
ReferenceEquals(value, null)
```

A null assignment is allowed only when null is a valid domain or transport state. The restriction applies primarily to null comparison syntax.

### 3.4 LINQ

- Use LINQ for collection filtering, projection, grouping, ordering, aggregation, and set operations when it improves clarity.
- Avoid manual loops when a clear LINQ expression exists.
- Do not force LINQ into stateful workflows, side-effect-heavy code, or logic that becomes harder to debug.
- Avoid repeated enumeration.
- Materialize intentionally with `ToListAsync`, `ToArray`, or `ToDictionaryAsync`.
- Prefer database-translatable LINQ for EF Core queries.

### 3.5 Member order

Within a class, use this order:

1. Constants.
2. Static fields.
3. Instance fields.
4. Constructors.
5. Properties.
6. Private methods.
7. Protected methods.
8. Public methods.

Within each group, keep members ordered by workflow or responsibility.

The required method visibility order is:

```text
private
protected
public
```

Do not mix visibility levels randomly.

### 3.6 Services and responsibilities

- External integrations must be implemented behind dedicated interfaces.
- Application logic must not instantiate `HttpClient`, SDK clients, database connections, clocks, file systems, or message brokers directly.
- Use dependency injection.
- One class must have one clear responsibility.
- Do not make members `public` just so tests can reach them.
- Public members are only for API contracts, DI entry points, framework entry points, or behavior intentionally exposed by a class contract.
- Keep implementation helpers `private`.
- If unit tests need to substitute behavior, extract a focused interface and test through the public contract that uses it.
- Unit tests must mock or fake dependencies through interfaces, not by opening visibility on implementation details.
- Favor code reuse when the same business rule, validation, mapping, tenant check, permission check, or integration behavior appears in more than one place.
- Extract reusable code into cohesive value objects, validators, mappers, policies, adapters, or focused services owned by the correct bounded context.
- Avoid generic service classes named `Helper`, `Manager`, or `Utils`.
- Focused validation helpers are allowed.
- Helper methods must be pure when possible.
- Do not create static global state.

### 3.7 Date and time

- Business code must not call `DateTime.Now`, `DateTime.UtcNow`, or `DateTimeOffset.UtcNow` directly.
- Inject `IDateTimeProvider`.
- Persist UTC timestamps.
- Use `DateTimeOffset`.
- Tests must use a deterministic fake date-time provider.

### 3.8 Exceptions

- Business and application code must throw typed project exceptions.
- Do not write repeated exception messages inline.
- Exception messages, error codes, and contextual data belong to the exception class.
- Preserve the original exception as `InnerException` when wrapping infrastructure failures.
- Map exceptions to RFC 7807 Problem Details in one global exception handler.
- Do not catch an exception only to log and rethrow it unchanged.

### 3.9 Results

- A method must return one declared result type.
- Do not return unrelated shapes from different branches.
- API endpoints must return explicit response contracts or a documented result union.
- Application services must not return EF Core entities.
- Use immutable records for requests, responses, and integration events.

---

## 4. Multi-tenant rules

- Never trust `TenantId` from a request body.
- Resolve tenant context from authenticated and authorized information.
- Assign `TenantId` server-side.
- Prevent `TenantId` modification.
- Validate store ownership under the current tenant.
- Validate all related IDs against the current tenant.
- Every tenant-owned feature requires a negative cross-tenant test.
- Public queries must expose only published and discoverable fields.
- `IgnoreQueryFilters()` must be followed by an explicit safe predicate in the same query.
- Integration events containing private data must include `TenantId`.

---

## 5. External services

Every external service must have:

- An application-layer interface.
- An infrastructure-layer implementation.
- Typed configuration validated at startup.
- A typed `HttpClient` or dedicated SDK wrapper.
- Cancellation support.
- Timeout and resilience configuration.
- Structured logging.
- Typed project exceptions.
- A dedicated xUnit test class.
- Tests for success and failure paths.
- No secrets in source code or logs.

Read `.ai/skills/create-external-service-client/SKILL.md`.

---

## 6. Mandatory test coverage by subject

A dedicated xUnit test class is required for:

- Every external service adapter.
- Every create use case.
- Every update use case.
- Every delete or deactivate use case.
- Every main query service.
- Every wizard or multi-step process.
- Every workflow state transition handler.
- Every authorization service.
- Every tenant resolver.
- Every exception-to-Problem-Details mapping.
- Every message consumer.
- Every important domain service.

Tests must cover:

- Expected success.
- Invalid input.
- Missing data.
- Unauthorized access.
- Cross-tenant access.
- Dependency failure.
- Cancellation when applicable.
- Boundary values.
- Idempotency when applicable.


## 6.1 Visibility and member order

- Do not make every class or member public by default.
- Public is for the API surface: controllers, DTOs, contracts/interfaces, public exception types, and intentionally reusable types.
- Keep implementation details internal, private, or protected as appropriate.
- Never widen visibility only to make tests compile.
- Prefer testing through contracts and abstractions. If mocking is impossible, extract a focused interface instead of using a forced integration test.
- Use private helpers for local logic and static helper classes for cohesive validation utilities.
- Validators are helpers in this project. Do not create validator interfaces unless there is a real runtime need to swap implementations.
- Keep validation rules in one place. Do not duplicate slug/name rules across models, services, and tests.
- Avoid partial classes and generated regex unless there is a strong reason. Prefer simple readable validation code or a shared common helper when reuse is real.
- Order members consistently: constants/fields, constructors, public methods/properties, protected members, private helpers.

---

## 7. Test conventions

- Use xUnit.net v3.
- Use NSubstitute for substitutions.
- Use built-in xUnit assertions by default.
- Default to unit tests only unless the user explicitly asks for integration tests.
- Controller tests instantiate the controller directly and substitute the service contract. Do not use full-pipeline hosts, `HttpClient`, databases, or auth handlers for unit tests.
- Service tests use NSubstitute against contracts such as repositories, Unit of Work, current-user context, time providers, and external clients.
- If code cannot be unit-tested with mocks or fakes, introduce a small cohesive abstraction instead of forcing an integration test.
- Do not mock `DbSet`.
- Do not use EF Core InMemory as a shortcut for persistence behavior.
- Test behavior, not private implementation details.
- Do not change private, protected, or internal members to public for test access.
- Keep production visibility as narrow as the design allows; do not make classes or members public only because tests need access.
- Use public for API surface, DTOs, contracts, controllers, and intended reusable types. Keep implementation details internal or private when possible.
- Prefer a readable member order: public API first, protected extension points next, private helpers last. Fields/constants stay near the top.
- Unit-test business logic through service interfaces and substituted dependencies.
- Use deterministic IDs, clocks, and inputs.
- Follow `MethodName_State_ExpectedResult` naming.
- Each test follows Arrange, Act, Assert.
- One behavioral reason to fail per test.

---

## 8. Wizard and process rules

A wizard or multi-step process must have:

- An explicit state model.
- An explicit list of valid transitions.
- A process service or state machine.
- Persistence of current state when required.
- Idempotency for repeated commands.
- Tenant validation.
- Authorization validation.
- Deterministic time through `IDateTimeProvider`.
- Typed exceptions for invalid transitions.
- Unit tests for every valid and invalid transition.
- Unit tests for persistence-facing decisions through abstractions. Full persistence/resume integration tests are deferred until explicitly requested.

Read `.ai/skills/create-wizard-process/SKILL.md`.

---

## 9. Required workflow for any task

1. Restate the requested behavior.
2. Check branch and working tree status when code changes are requested.
3. Identify or create the feature branch when the user asks the agent to manage Git workflow.
4. Identify the owning feature area.
5. Identify the tenant boundary.
6. Identify the required permission.
7. Identify external dependencies.
8. Identify required exception types.
9. Identify required tests.
10. Inspect existing patterns.
11. Identify repeated logic and reuse existing cohesive abstractions when appropriate.
12. Implement the smallest complete vertical slice.
13. Apply the backend and infrastructure checklists when relevant.
14. Run formatting.
15. Run build.
16. Run unit tests.
17. Run integration tests only when the task explicitly added or requested them.
18. Update OpenAPI and documentation.
19. Add or update Postman functional requests when HTTP endpoints are added or changed.
20. Update `.ai/CONTEXT_MAP.md` when structure, entry points, standard commands, or important decisions changed.
21. Use small coherent commits if the user requested commits.
22. Summarize changed files, decisions, test results, branch/commit status, and remaining risks.

---

## 10. Forbidden behavior

Do not:

- Create one microservice per table.
- Create one API project per feature area during the MVP.
- Create separate Domain/Application/Infrastructure projects at the beginning.
- Create one DbContext, repository, or EF configuration class per entity before it is needed.
- Create one repository interface per entity when it only repeats generic CRUD.
- Use lazy loading.
- Put business logic in endpoints.
- Return persistence entities from APIs.
- Use early returns.
- Compare null using `==` or `!=`.
- Call the system clock directly.
- Instantiate external clients inside business classes.
- Write repeated exception messages inline.
- Duplicate business rules, validation, mapping, tenant checks, permission checks, or integration behavior when a cohesive reusable component exists or should be extracted.
- Add a dependency without justification.
- Remove tests to make a pipeline pass.
- suppress warnings without documenting the reason.
- modify unrelated files.
- mix unrelated features in the same feature branch.
- commit unrelated working tree changes.
- push a branch unless the user explicitly asks.
- invent requirements.
- leave `.ai/CONTEXT_MAP.md` stale after adding or moving important projects, entry points, commands, or architecture decisions.
- leave changed public API endpoints undocumented in Postman while there is no frontend, unless explicitly excluded.
- mark work complete while build or applicable tests fail.

---

## 11. Completion report

When finishing a task, report:

- Owning feature area.
- Behavior implemented.
- Files changed.
- Tenant and authorization protections.
- Exceptions added.
- Tests added.
- Commands executed.
- Build result.
- Test result.
- Known limitations.
