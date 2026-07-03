---
name: create-crud-use-case
description: Implement or modify one OpenStore create, read, update, delete, deactivate, archive, or query use case inside an existing bounded context. Use when a request needs endpoint, application service, contracts, validation, persistence, authorization, tenant or store isolation, typed exceptions, tests, and Postman functional requests.
---

# Create CRUD Use Case

## Objective

Implement a create, read, update, delete, deactivate, or archive use case with complete validation, tenancy, authorization, exceptions, and tests.

Prefer `create-feature-slice` when the request is the first end-to-end feature in a context or the required project structure does not exist yet.

## Required inputs

- Owning service.
- Entity or aggregate.
- Operation.
- Required permission.
- Request contract.
- Response contract.
- Business rules.
- Tenant and store scope.

If an input is missing, infer it from existing code and documentation when safe. Ask only when the operation, permission, or tenant boundary cannot be inferred without inventing requirements.

## Steps

1. Read mandatory documentation.
2. Identify the bounded context, aggregate owner, tenant boundary, store boundary, and required permission.
3. Inspect existing endpoint, application, domain, infrastructure, and test patterns in the owning context.
4. Define explicit request and response records in the application or API contract location used by the context.
5. Define or reuse typed exceptions for validation, missing resources, tenant access, authorization, and dependency failures.
6. Implement focused validation helpers or domain factory validation.
7. Implement the application use case with tenant and store ownership checks before mutation.
8. Implement authorization through the existing project abstraction.
9. Implement persistence in the owning infrastructure project without exposing EF entities.
10. Map explicitly to response contracts.
11. Add or update controller actions and Postman functional requests when HTTP behavior changes.
12. Add a dedicated xUnit test class named `<UseCaseOrServiceName>Tests`.
13. Add success, invalid input, missing resource, unauthorized, cross-tenant, dependency failure, and cancellation tests when applicable.
14. Add unit tests through contracts and substituted dependencies. If persistence or full HTTP pipeline coverage is truly required, stop and ask for an explicit integration-test decision.
15. Run formatting, build, and unit tests. Run integration tests only when explicitly requested.
16. Update documentation only when public behavior, contracts, or architecture changed.

## Expected file shape

Adapt names to the existing context, but prefer this shape for new code:

```text
src/Services/<Context>/
  OpenStore.<Context>.Api/<Feature>/<Operation>Endpoint.cs
  OpenStore.<Context>.Application/<Feature>/<Operation>Request.cs
  OpenStore.<Context>.Application/<Feature>/<Operation>Response.cs
  OpenStore.<Context>.Application/<Feature>/<Operation>Service.cs
  OpenStore.<Context>.Domain/<Aggregate>/
  OpenStore.<Context>.Infrastructure/
tests/
  OpenStore.<Context>.UnitTests/<Feature>/<Operation>ServiceTests.cs
  tests/OpenStore.Api.Tests/<Context>/<Area>/<Subject>Tests.cs
```

## Mandatory code style

- One return statement.
- No early returns.
- `is null` and `is not null`.
- Private, protected, then public method order.
- LINQ when clearer.
- `IDateTimeProvider` for timestamps.
- Typed exceptions.
- No inline repeated exception messages.
- No EF entities returned by the API.
- Persisted entities use `long Id` internally; expose `Guid PublicId`, slugs, or other public identifiers through API contracts when needed.

## Completion report

Report:

- Owning service and bounded context.
- Behavior implemented.
- Tenant, store, and authorization protections.
- Exceptions added or reused.
- Tests added.
- Commands executed and results.
- Known limitations or risks.

## Definition of done

The use case is secure, tenant-aware, tested, documented, and follows the approved code style.
