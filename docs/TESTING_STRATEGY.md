# Testing Strategy

Automated tests are mandatory for important backend behavior.

---

## 1. Current Approach - MVP Phase

During the MVP, all automated tests are **unit tests** that use mocks/fakes through interfaces:

- xUnit.net v3
- NSubstitute
- Built-in xUnit assertions

Controllers are tested by instantiating them directly and mocking their service dependencies. No full-pipeline host, no real database, no test authentication handler.

API/integration tests using the full ASP.NET pipeline are deferred until explicitly requested in a future task.

Postman collections remain the functional/manual API testing surface while no frontend exists.

---

## 2. Test Project Structure

Use one test project at the beginning:

```text
tests/
  OpenStore.Api.Tests/
    OpenStore.Api.Tests.csproj
```

Tests mirror the production folder structure.

Example:

```text
src/
  OpenStore.Api/
    Tenancy/
      Controllers/
        TenantsController.cs
      Services/
        TenantService.cs
tests/
  OpenStore.Api.Tests/
    Tenancy/
      Controllers/
        TenantsControllerTests.cs
      Services/
        TenantServiceTests.cs
      Validators/
        TenantValidatorTests.cs
```

The differences are:

- Tests start under `tests/OpenStore.Api.Tests`.
- Test namespaces start with `OpenStore.Api.Tests`.
- Test files end with `Tests.cs`.
- Test classes end with `Tests`.

Do not create separate `UnitTests`, `IntegrationTests`, `Api`, or `Persistence` folders at the beginning.

---

## 3. Namespace Convention

Namespaces mirror the same logical path.

Production:

```csharp
namespace OpenStore.Api.Tenancy.Services;
```

Test:

```csharp
namespace OpenStore.Api.Tests.Tenancy.Services;
```

Production:

```csharp
namespace OpenStore.Api.Tenancy.Controllers;
```

Test:

```csharp
namespace OpenStore.Api.Tests.Tenancy.Controllers;
```

---

## 4. Required Test Classes

A production class with significant behavior should normally have a corresponding test class named:

```text
<ProductionClassName>Tests
```

The test file must be named:

```text
<ProductionClassName>Tests.cs
```

Examples:

```text
TenantService.cs -> TenantServiceTests.cs
TenantsController.cs -> TenantsControllerTests.cs
TenantValidator.cs -> TenantValidatorTests.cs
```

---

## 5. Required Coverage

Test important behavior for:

- Create use cases.
- Update use cases.
- Delete, deactivate, or archive use cases.
- Main read/query services.
- Controllers with HTTP behavior.
- Tenant isolation.
- Authorization.
- Validation helpers.
- Important domain or business services.
- External adapters.
- Exception mapping.

---

## 6. CRUD Coverage

### Create

Test:

- Valid creation.
- Required fields.
- Invalid values.
- Duplicate values.
- Tenant assignment.
- Related entity ownership.
- Authorization.
- Persistence failure when relevant.
- Date assignment through `IDateTimeProvider`.

### Read

Test:

- Existing resource.
- Missing resource.
- Tenant filtering.
- Public visibility.
- Pagination.
- Ordering.
- Projection.

### Update

Test:

- Valid update.
- Missing resource.
- Invalid values.
- Tenant mismatch.
- Unauthorized role.
- Immutable fields.
- Concurrency when implemented.
- Updated timestamp.

### Delete Or Deactivate

Test:

- Valid deletion or deactivation.
- Missing resource.
- Tenant mismatch.
- Unauthorized role.
- Referential constraints.
- Idempotent repeated request when intended.

---

## 7. Tenant Isolation Tests

Every tenant-owned feature requires at least one negative test:

```text
Given resource belongs to Tenant A
When authenticated user belongs to Tenant B
Then resource is not returned or modified
```

Prefer `NotFound` over revealing resource existence when required by the security design.

---

## 8. Test Naming

Use:

```text
MethodName_State_ExpectedResult
```

Examples:

```csharp
CreateAsync_ValidRequest_CreatesTenant
CreateAsync_DuplicateSlug_ThrowsDuplicateTenantSlugException
CreateAsync_AuthenticatedUser_CreatesOwnerMembership
```

---

## 9. Test Structure

Every test follows:

```text
Arrange
Act
Assert
```

Use explicit sections or blank-line separation.

A test should have one behavioral reason to fail.

Avoid:

- Shared mutable state.
- Random data without a fixed seed.
- Dependence on system time.
- Dependence on test execution order.
- Real external provider calls in unit tests.
- Mocking EF Core internals.

---

## 10. Unit Tests

Unit tests should:

- Be fast.
- Avoid network and real databases.
- Substitute interfaces.
- Use a fake `IDateTimeProvider`.
- Verify behavior and observable interactions.
- Avoid testing private methods directly.
- Never make a private helper public only to unit-test it.
- When behavior must be isolated, introduce a focused interface and substitute that interface in tests.

Do not assert every internal call. Assert only meaningful collaboration.

Controller unit tests instantiate the controller directly and mock its service dependencies. Do not use full-pipeline hosts, `HttpClient`, real databases, or test authentication handlers in unit tests.

Services, validators, and other application logic are tested through their public contracts with substituted dependencies.

---

## 11. Integration Tests - Deferred

API/integration tests that exercise the full ASP.NET pipeline are deferred until explicitly requested.

When they are added, choose the test host, database strategy, authentication handling, and dependency replacement approach intentionally for that task. Do not assume a default integration-test stack.

No integration tests exist yet. Do not add them without a specific task.

---

## 12. Functional API Testing With Postman

Until OpenStore has a frontend, Postman is the functional testing and request-documentation surface for HTTP APIs.

Every implemented API endpoint should have a documented Postman request when the API exists.

Postman coverage should include:

- Success request.
- Validation failure request.
- Unauthorized request.
- Forbidden request when permission matters.
- Missing resource request when applicable.
- Conflict or duplicate request when applicable.
- Cross-tenant request when tenant-owned data is involved.

Postman requests must use environment variables for base URLs, tokens, tenant IDs, store IDs, product IDs, slugs, and other changing values.

Do not commit real tokens, secrets, passwords, private URLs, or production credentials.

Postman tests should include basic status-code and response-shape assertions. Complex business verification remains in xUnit tests.

See `docs/POSTMAN_FUNCTIONAL_TESTING.md`.

---

## 13. Test Completion Checklist

- [ ] Test folder mirrors the production folder.
- [ ] Test namespace mirrors the production namespace with `OpenStore.Api.Tests`.
- [ ] Test file ends with `Tests.cs`.
- [ ] Test class ends with `Tests`.
- [ ] Success path exists.
- [ ] Validation failure exists.
- [ ] Missing resource exists when applicable.
- [ ] Unauthorized path exists when applicable.
- [ ] Cross-tenant path exists when tenant-owned behavior exists.
- [ ] Dependency failure exists when applicable.
- [ ] Cancellation exists when applicable.
- [ ] Time is deterministic.
- [ ] Test names follow the convention.
- [ ] Build passes.
- [ ] Tests pass.
- [ ] Postman request added or updated for changed API endpoints.
- [ ] Postman environment variables do not contain real secrets.
