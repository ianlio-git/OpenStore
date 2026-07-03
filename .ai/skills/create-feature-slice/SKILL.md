---
name: create-feature-slice
description: Implement the first or next complete OpenStore vertical feature slice inside the modular monolith. Use for MVP work such as register user, create tenant, create store, create product, publish catalog, public catalog read, WhatsApp cart validation, invitations, roles, or any end-to-end feature that needs API, application service, domain model, persistence, tenant/store isolation, authorization, typed exceptions, tests, and Postman functional requests without creating a new microservice or extra API project.
---

# Create Feature Slice

## Objective

Build the smallest complete end-to-end feature inside the simple API structure while preserving a future path to service extraction.

Use this before `create-microservice` during early MVP work. Create only one API project for the MVP unless the user explicitly approves an ADR for an independently deployable service.

## Required inputs

- Feature name.
- Owning feature area.
- User or actor.
- Required permission.
- Tenant scope.
- Store scope, if any.
- Request contract.
- Response contract.
- Business rules.
- Persistence changes.
- Public visibility rules, if any.
- External dependencies, if any.

Infer missing details from README, ADRs, docs, and existing code when safe. Ask only when the operation, permission, tenant boundary, or business rule would otherwise be invented.

## Steps

1. Read `README.md`, `AGENTS.md`, architecture, coding standards, testing strategy, exception handling, related ADRs, and existing patterns in the target context.
2. Restate the requested behavior in one short paragraph for the working notes or final summary.
3. Read `docs/GIT_WORKFLOW.md` when code changes or commits are requested.
4. Identify the owning feature area and explain why it owns the behavior.
5. Identify tenant boundary, store boundary, required permission, external dependencies, required exceptions, and required tests.
6. Inspect existing project structure before creating files.
7. Add or update domain model, value objects, validation helpers, and invariants.
8. Add or update application request and response contracts.
9. Add or update the application service or use case.
10. Add authorization and tenant/store ownership checks before accessing or mutating private data.
11. Add or update infrastructure persistence: create an `IEntityTypeConfiguration<T>` class inside the feature's `Persistence/` folder, register it in `Common/Persistence/ModelBuilderConfigurationExtensions.ApplyOpenStoreConfigurations()`, and do not add `HasQueryFilter(x => x.IsActive)` (it is applied centrally).
12. Add or update thin controller actions and Postman functional requests when HTTP behavior changes.
13. Add explicit mapping methods; do not return EF entities from APIs.
14. Add focused unit tests for application and domain behavior.
15. Add PostgreSQL persistence tests when schema or query behavior changes.
16. Add direct controller unit tests for endpoint behavior. Ask before adding full API integration tests.
17. Add negative cross-tenant tests for every tenant-owned behavior.
18. Add or update Postman requests and environment variables when HTTP endpoints are added or changed.
19. Run formatting, build, and unit tests. Run integration tests only when explicitly requested.
20. Update documentation only when public behavior, contracts, architecture, or API functional testing changed.

## Preferred shape

Adapt to the existing repository, but prefer this initial shape:

```text
OpenStore.sln
src/
  OpenStore.Api/
    Common/
      Contracts/
      Entities/
      Persistence/
      Services/
      Time/
      Errors/
    <FeatureArea>/
tests/
  OpenStore.Api.Tests/
postman/
  OpenStore.postman_collection.json
  environments/
    local.postman_environment.json
```

For `CreateTenant`, keep the first files easy to scan:

```text
Tenancy/
  Controllers/
    TenantsController.cs
  Services/
    TenantService.cs
  Contracts/
    ITenantService.cs
  Models/
    Tenant.cs
    TenantMembership.cs
  Dtos/
    CreateTenantRequest.cs
    CreateTenantResponse.cs
```

Use controllers for HTTP at the beginning.
Controllers should depend on feature-area contracts such as `ITenantService`.
Tests must mirror the production folder structure under `tests/OpenStore.Api.Tests`, with files ending in `Tests.cs`.

Keep reusable generic code in `src/OpenStore.Api/Common/` only when it is useful across feature areas. Do not put feature-specific business rules in Common.

Use one `AppDbContext` at the beginning. Put entity-specific EF mappings inside each feature's `Persistence/` folder in dedicated `IEntityTypeConfiguration<T>` classes. Register them in `ModelBuilderConfigurationExtensions.ApplyOpenStoreConfigurations()`. Soft-delete query filters are applied centrally - do not repeat per entity.

Use `EntityServiceBase<TEntity>` as a base class for feature services that need persistence helpers. Common CRUD signatures live in `ICrudService<TResponse, TCreateRequest, TUpdateRequest>` and `IChildCrudService<TResponse, TCreateRequest, TUpdateRequest>` in `Common/Contracts`. Feature interfaces inherit these generic contracts and declare only feature-specific methods. `CreateAsync` normally returns the same response DTO used by `Get/Update` — create-specific DTOs are allowed only when they carry genuinely different data.

There is no generic `IEntityService` - `Repository<T>` is the generic data access abstraction. Do not create `TenancyDbContext`, `TenantRepository`, or feature-specific DbContexts unless the generic structure becomes insufficient.

## Constraints

- Do not create a new microservice unless an ADR-worthy independent boundary exists.
- Do not create one API project per module during the MVP.
- Do not create a service, module, or project for one table.
- Use `IRepository<TEntity>`, `Repository<TEntity>`, `IUnitOfWork`, `UnitOfWork`, `EntityServiceBase<TEntity>`, and one `AppDbContext` for repeated CRUD.
- Feature service contracts inherit `ICrudService<TResponse, TCreateRequest, TUpdateRequest>` or `IChildCrudService<TResponse, TCreateRequest, TUpdateRequest>` from `Common/Contracts` and add only feature-specific methods.
- `CreateAsync` normally returns the same response DTO used by `Get/Update`. A create-specific response DTO is allowed only when it contains genuinely different data.
- Do not create a specific repository if the generic repository can solve the query clearly.
- There is no generic `IEntityService` - feature services use `EntityServiceBase<TEntity>` as a base class or `IRepository<TEntity>` directly for data access.
- Define a feature-specific service contract when a controller calls business logic, for example `ITenantService`.
- Do not make methods, properties, or classes public just so tests can access them.
- Keep implementation helpers private.
- If tests need a mock or fake, depend on a focused interface and inject it.
- Unit tests must verify behavior through public contracts and substituted dependencies, not private implementation details.
- Do not trust `TenantId` or `StoreId` from request bodies.
- Do not create separate Domain/Application/Infrastructure projects at the beginning.
- Do not put business logic in controllers.
- Do not expose persistence entities from APIs.
- Identity rule: persisted entities use `long Id` internally; expose `Guid PublicId`, slugs, or other public identifiers through APIs when needed.
- Database scripts, migrations, and EF mappings must not use GUID primary keys by default.
- Do not add a mapper, mediator, validator, ORM, broker, or test framework without an approved ADR.
- Follow one-return, null-style, member-order, typed-exception, and deterministic-time rules.
- Keep all files for the feature on the same feature branch.
- Do not mix unrelated modules or features.
- Use small coherent commits only when the user asks the agent to commit.

## Completion report

Report:

- Owning feature area.
- Behavior implemented.
- Tenant, store, and authorization protections.
- Exceptions added or reused.
- Persistence and migration changes.
- Tests added.
- Postman requests added or updated for API endpoints.
- Commands executed and results.
- Branch and commit status when Git workflow was requested.
- Known limitations or risks.

## Definition of done

The feature works end to end, remains inside the correct feature area, preserves tenant and store isolation, exposes explicit contracts, includes required tests, includes Postman functional requests for changed API endpoints, and passes build and applicable tests.
