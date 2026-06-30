# Infrastructure

OpenStore infrastructure must be boring, explicit, observable, reproducible, and secure by default.

The architecture starts as a modular monolith. Infrastructure boundaries must preserve the future path to independently deployable bounded-context services without creating that operational complexity on day one.

---

## 1. Principles

- Prefer simple infrastructure until the product proves the need for more moving parts.
- Keep local development reproducible with simple .NET commands first; add orchestration only when the service set requires it.
- Keep self-hosted deployment reproducible with Docker Compose when deployment infrastructure is introduced.
- Make every service observable through logs, metrics, traces, health checks, and correlation IDs.
- Store secrets outside source control.
- Keep module ownership and database ownership explicit.
- Add asynchronous infrastructure only when a business workflow requires it.

---

## 2. Environments

OpenStore recognizes these environment types:

| Environment | Purpose | Requirements |
|---|---|---|
| Local | Developer workflow | API project, local containers, seeded test data, safe fake providers. |
| Test | Automated validation | Deterministic unit tests, direct controller tests, substituted dependencies, no live external providers. |
| Staging | Production-like verification | Real deployment shape, isolated data, safe provider accounts. |
| Production | Live users | Hardened secrets, backups, monitoring, alerts, rollback path. |

Environment-specific configuration must not change application behavior except through explicit options.

---

## 3. Local development

Use the API project as the first local development entrypoint.

Add Aspire AppHost only after the first runnable API exists and there is something meaningful to orchestrate.

Local development should provide:

- PostgreSQL containers for module databases or schemas.
- RabbitMQ only after an asynchronous workflow exists.
- OpenTelemetry collector when useful for tracing local distributed flows.
- Local service discovery through Aspire only after Aspire is introduced.
- Seed data for development only.
- Fake or sandbox external providers.

Local setup must not require production secrets.

---

## 4. Self-hosted deployment

Docker Compose is a future self-hosted deployment path, introduced when deployment work starts.

Compose files should include:

- API or modular backend service containers.
- Gateway container only when public routing across multiple backend processes is needed.
- Frontend container only after a frontend exists.
- PostgreSQL per bounded-context module or service, depending on the current deployment stage.
- RabbitMQ only when messaging is introduced.
- OpenTelemetry collector or compatible observability agent.
- Explicit volumes for durable data.
- Health checks for every long-running service.

Do not hide production-only behavior inside local-only Compose overrides.

---

## 5. Configuration

Use typed options for every configurable integration.

Configuration rules:

- Validate options at startup.
- Fail fast when required configuration is missing.
- Keep defaults safe for local development.
- Keep production settings explicit.
- Do not read raw environment variables from application services.
- Do not commit secrets, tokens, passwords, private keys, or provider credentials.
- Prefer namespaced settings per bounded context and provider.

Example naming:

```text
OpenStore__Catalog__Database__ConnectionString
OpenStore__Notifications__Email__BaseUrl
OpenStore__Search__Maps__TimeoutSeconds
```

---

## 6. Secrets

Secrets must live outside the repository.

Allowed local sources:

- .NET user secrets.
- Local environment variables.
- Local secret manager supported by Aspire.

Allowed production sources:

- Cloud secret manager.
- Container orchestrator secrets.
- Environment variables injected by deployment automation.

Never log secrets. Never include secrets in exception messages, telemetry attributes, Problem Details, OpenAPI examples, tests, screenshots, or documentation.

---

## 7. Databases

Each module owns its data boundary. Each independently deployable service later owns its database.

During modular-monolith stages, keep ownership explicit even if modules run in one process.

Rules:

- No cross-module foreign keys.
- No module queries another module's private persistence directly.
- No shared `DbContext` across modules.
- Migrations live in the owning module.
- Tenant-aware indexes are required for tenant-owned data.
- Backups and restore procedures must be documented before production.
- Migration execution must be repeatable and observable.

---

## 8. Messaging

Do not introduce RabbitMQ until a business flow requires asynchronous communication.

When messaging is introduced:

- Use immutable versioned integration events.
- Use an outbox when message loss is unacceptable.
- Use inbox or idempotency storage for consumers.
- Include `TenantId` when events contain private tenant data.
- Keep consumers idempotent.
- Add explicit messaging tests only when RabbitMQ behavior is implemented and the user requests integration coverage.
- Document retry, dead-letter, and replay behavior.

---

## 9. Gateway and public routing

Use YARP when multiple backend services require public routing.

Gateway rules:

- Keep routing explicit.
- Do not put business logic in the gateway.
- Preserve correlation IDs.
- Forward authentication context safely.
- Enforce TLS at the deployment boundary.
- Rate-limit public endpoints when abuse risk exists.

Public catalog and public cart endpoints must expose only safe projections.

---

## 10. Observability

Every service must emit:

- Structured logs.
- Metrics.
- Distributed traces.
- Health checks.
- Correlation or trace identifiers.

Logging rules:

- Log unexpected exceptions once at the boundary.
- Use structured properties.
- Do not log secrets or unnecessary personal data.
- Include tenant and store identifiers only when safe and useful.

Health check rules:

- Liveness checks confirm the process is running.
- Readiness checks confirm required dependencies are reachable.
- Startup checks confirm migrations/configuration are valid when applicable.

---

## 11. CI expectations

CI should eventually run:

- Restore with locked dependencies.
- Format check.
- Build with warnings as errors.
- Unit tests.
- Integration tests that can run in CI.
- Architecture tests.
- Dependency vulnerability scan.
- License compatibility scan.
- Container build.
- OpenAPI generation or validation.

Do not make CI depend on live external provider accounts.

---

## 12. Deployment quality bar

Before production, OpenStore must have:

- Repeatable deployment.
- Rollback plan.
- Database backup and restore verification.
- Migration plan.
- Health checks.
- Centralized logs.
- Metrics and traces.
- Alerting for availability, error rate, latency, and storage.
- Secret rotation process.
- Dependency and container scanning.
- Documented operational runbook.

---

## 13. Infrastructure change checklist

Use `.ai/checklists/infrastructure-change.md` for infrastructure-impacting changes.

An infrastructure change is complete only when it is reproducible, documented, observable, secure, and tested at the appropriate level.
