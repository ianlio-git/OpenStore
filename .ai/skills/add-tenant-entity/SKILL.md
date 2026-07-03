---
name: add-tenant-entity
description: Add or modify an OpenStore tenant-owned or store-owned entity, EF Core configuration, indexes, migrations, query filters, safe server-side TenantId assignment, and cross-tenant tests. Use when a feature introduces private tenant data, store-scoped data, or persistence rules involving TenantId or StoreId.
---

# Add Tenant-Owned Entity

## Objective

Add a tenant-owned entity with safe persistence, indexing, querying, and tests.

Use this skill together with `create-crud-use-case` or `create-feature-slice` when the entity is introduced as part of a user-facing operation.

## Required inputs

- Owning bounded context.
- Entity name and purpose.
- Tenant scope.
- Store scope, if any.
- Uniqueness rules.
- Public visibility rules, if any.
- Lifecycle rules.

## Steps

1. Confirm the owning service.
2. Confirm whether the entity is tenant-owned, store-owned, public, or a projection.
3. Use the shared identity convention: `long Id` internal primary key, `Guid PublicId` only when an external identifier is needed.
4. Inherit from `BaseEntity`; implement `ITenantEntity` for private tenant-owned data. Do not recreate `BaseTenantEntity`.
5. Add required `StoreId` and store ownership validation when the entity belongs to a store.
6. Add domain invariants or factory validation for required fields and lifecycle rules.
7. Add EF Core mapping in a feature-level `Persistence/` `IEntityTypeConfiguration<TEntity>` class and register it in `ModelBuilderConfigurationExtensions.ApplyOpenStoreConfigurations()`.
8. Add tenant-aware indexes and tenant-aware unique constraints.
9. Do not add per-entity soft-delete query filters; `BaseEntity.IsActive` filters are applied centrally through `SoftDeleteModelBuilderExtensions`.
10. Ensure `TenantId` and `StoreId` are internal numeric foreign keys assigned from trusted server context, not request bodies.
11. Prevent `TenantId` and `StoreId` modification after creation unless an explicit transfer workflow exists.
12. Add migration or SQL script only when explicitly requested; otherwise keep Development schema creation through EF models.
13. Add unit tests for tenant/store rules through service abstractions.
14. Add at least one negative cross-tenant unit test for read and mutation behavior when applicable.
15. Run formatting, build, and unit tests. Run integration tests only when explicitly requested.

## Constraints

- No trusted `TenantId` from clients.
- No GUID primary keys by default.
- No public API exposure of internal numeric IDs by default.
- No database script or migration that uses public GUIDs as foreign keys when an internal numeric key exists.
- No cross-service database relationship.
- No API exposure of the EF entity.
- No `IgnoreQueryFilters()` without an explicit safe predicate.
- No global unique constraint when tenant-specific uniqueness is required.
- No public projection exposing internal membership, billing, cost, or permission data.

## Completion report

Report the entity, owning context, identity shape (`Id`/`PublicId`), tenant/store rules, indexes, migration or schema decision, tests, and validation commands.
