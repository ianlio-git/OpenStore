# ADR-0001: Initial Architecture

## Status

Accepted.

## Context

OpenStore must support multiple organizations, multiple stores per organization, users with tenant-level and store-level roles, public catalogs, WhatsApp carts, local discovery, and future operational workflows.

## Decision

Use a monorepo with bounded-context microservices, PostgreSQL per service, HTTP for direct synchronous requests, and RabbitMQ integration events only when asynchronous workflows are justified.

Use .NET Aspire for local orchestration and Docker Compose for the initial self-hosted deployment path.

## Consequences

Positive:

- Clear ownership.
- Strong tenant boundaries.
- Independent service evolution.
- Explicit integration contracts.
- A path toward independent scaling.

Negative:

- Greater operational complexity.
- Eventual consistency between services.
- More integration tests.
- More deployment components.

## Guardrail

Do not create all target services immediately.

Implement the smallest complete vertical slice. A bounded context may remain an internal module until independent deployment provides measurable value.
