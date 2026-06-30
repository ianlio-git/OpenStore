---
name: write-tests
description: Create or improve OpenStore xUnit.net v3 unit, integration, persistence, API, external adapter, authorization, tenant isolation, workflow, or exception mapping tests. Use when behavior needs test coverage, a bug needs regression tests, or a feature is incomplete without required success, failure, authorization, cross-tenant, cancellation, and dependency-failure coverage.
---

# Write Tests

## Objective

Create complete xUnit.net v3 tests for a production subject.

## Required inputs

- Production class.
- Behavior.
- Dependencies.
- Tenant scope.
- Permission scope.
- Failure modes.

Infer missing inputs from production code when safe. Ask only when expected behavior is ambiguous.

## Steps

1. Read the production subject and adjacent existing tests before writing new tests.
2. Create or update `<ProductionClassName>Tests`.
3. Use NSubstitute for interfaces.
4. Use a deterministic `IDateTimeProvider`.
5. Use deterministic IDs, tenant IDs, store IDs, and input data.
6. Add valid success tests.
7. Add validation tests.
8. Add missing-resource tests.
9. Add authorization tests.
10. Add cross-tenant tests for tenant-owned behavior.
11. Add dependency-failure tests.
12. Add cancellation tests when the production method accepts a `CancellationToken`.
13. Add boundary tests.
14. Add idempotency tests when applicable.
15. Add persistence-facing unit tests through abstractions. Ask before adding database integration tests.
16. Add direct controller unit tests for HTTP-facing behavior. Ask before adding full API integration tests.
17. Add WireMock.Net tests when HTTP provider behavior matters.
18. Run the narrow test project first, then broader tests when risk justifies it.

## Naming

```text
MethodName_State_ExpectedResult
```

## Location

Mirror the production folder structure under `tests/OpenStore.Api.Tests`.

Examples:

```text
src/OpenStore.Api/Tenancy/Controllers/TenantsController.cs
tests/OpenStore.Api.Tests/Tenancy/Controllers/TenantsControllerTests.cs
```

```text
src/OpenStore.Api/Tenancy/Services/TenantService.cs
tests/OpenStore.Api.Tests/Tenancy/Services/TenantServiceTests.cs
```

```text
src/OpenStore.Api/Tenancy/Models/TenantSlug.cs
tests/OpenStore.Api.Tests/Tenancy/Models/TenantSlugTests.cs
```

```text
src/OpenStore.Api/Tenancy/Validators/TenantValidator.cs
tests/OpenStore.Api.Tests/Tenancy/Validators/TenantValidatorTests.cs
```

Namespaces follow the same logical path, replacing `OpenStore.Api` with `OpenStore.Api.Tests`.

```csharp
namespace OpenStore.Api.Tenancy.Services;
```

```csharp
namespace OpenStore.Api.Tests.Tenancy.Services;
```

Do not create separate `UnitTests`, `IntegrationTests`, `Api`, or `Persistence` folders at the beginning.

Test files must end with `Tests.cs`.

## Constraints

- Do not test private methods directly.
- Do not mock `DbSet`.
- Do not use EF Core InMemory as relational proof.
- Do not depend on system time.
- Do not use live external providers.
- One behavioral reason to fail per test.

## Arrange, Act, Assert

Prefer explicit sections:

```csharp
// Arrange

// Act

// Assert
```

For exception tests, capture the exception in Act and assert its type, code, and important context.

## Completion report

Report test classes added or changed, behavior covered, commands executed, and any remaining coverage gaps.
