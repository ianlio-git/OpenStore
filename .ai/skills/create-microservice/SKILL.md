---
name: create-microservice
description: Create or split out an OpenStore bounded-context microservice with API, Application, Domain, Infrastructure, tests, PostgreSQL ownership, Aspire registration, OpenTelemetry, OpenAPI, health checks, gateway routing, ADR, and one complete vertical slice. Use only when an independent deployment or data boundary is justified; prefer create-feature-slice for early MVP work inside an existing modular service.
---

# Create Microservice

## Objective

Create a bounded-context microservice that follows OpenStore architecture, tenancy, security, code style, testing, observability, and deployment rules.

Do not use this skill just because a new table, endpoint, or feature is requested. Use it only when an ADR-worthy service boundary exists.

## Required inputs

- Service name.
- Business capability.
- Data owned by the service.
- APIs exposed.
- Integration events produced.
- Integration events consumed.
- Tenant requirements.
- Authorization requirements.
- Reason an existing service cannot own the capability.

If the reason for an independent service is weak, propose a module or feature slice inside an existing bounded context instead of creating the microservice.

## Steps

1. Read all mandatory project documentation.
2. Confirm the bounded context.
3. Confirm why an existing service or module cannot own the capability.
4. Create or update an ADR.
5. Define owned data and forbidden cross-service access.
6. Create:
   - API project.
   - Application project.
   - Domain project.
   - Infrastructure project.
   - Unit test project.
   - Unit tests in the shared test project; add a separate integration project only after explicit approval.
7. Configure approved project references.
8. Add PostgreSQL persistence.
9. Add tenant and store context when applicable.
10. Add health checks.
11. Add OpenTelemetry.
12. Add OpenAPI.
13. Add one complete vertical slice.
14. Add typed exceptions.
15. Add required test classes.
16. Add a negative tenant isolation test.
17. Register the service in Aspire.
18. Add gateway routing only when public routing is required.
19. Run formatting, build, and unit tests. Run integration tests only when explicitly requested.
20. Update documentation.

## Expected project references

```text
Api -> Application
Api -> Infrastructure
Application -> Domain
Infrastructure -> Application
Infrastructure -> Domain
Domain -> no other service layer
```

Cross-service domain references are forbidden.

## Constraints

- Do not create a service for one table.
- Do not share a database.
- Do not create cross-service foreign keys.
- Do not reference another service Domain project.
- Do not use a generic repository.
- Do not expose EF entities.
- Do not accept trusted `TenantId` from request bodies.
- Do not add a broker unless an asynchronous use case exists.
- Follow the single-return and member-order rules.

## Completion report

Report the ADR, projects created, owned data, service boundary, routes, tenant protections, tests, commands, and any operational follow-up.

## Definition of done

The service runs locally, owns its data, validates tenant and permission boundaries, exposes only approved contracts, includes required tests, and passes build and tests.
