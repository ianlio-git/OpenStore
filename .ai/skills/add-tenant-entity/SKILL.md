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
3. Inherit from `BaseTenantEntity` or implement `ITenantEntity` for private tenant-owned data.
4. Add required `StoreId` and store ownership validation when the entity belongs to a store.
5. Add domain invariants or factory validation for required fields and lifecycle rules.
6. Add EF Core `IEntityTypeConfiguration<TEntity>`.
7. Add tenant-aware indexes and tenant-aware unique constraints.
8. Add a query filter only when the service strategy uses filters consistently.
9. Ensure `TenantId` and `StoreId` are assigned from trusted server context, not request bodies.
10. Prevent `TenantId` and `StoreId` modification after creation unless an explicit transfer workflow exists.
11. Add migration in the owning service.
12. Add PostgreSQL persistence tests for schema, indexes, uniqueness, and query behavior.
13. Add at least one negative cross-tenant test for read and mutation behavior.
14. Run formatting, build, and unit tests. Run integration tests only when explicitly requested.

## Constraints

- No trusted `TenantId` from clients.
- No cross-service database relationship.
- No API exposure of the EF entity.
- No `IgnoreQueryFilters()` without an explicit safe predicate.
- No global unique constraint when tenant-specific uniqueness is required.
- No public projection exposing internal membership, billing, cost, or permission data.

## Completion report

Report the entity, owning context, tenant/store rules, indexes, migration, tests, and validation commands.
