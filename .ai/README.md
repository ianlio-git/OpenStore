# AI Skills Usage Guide

This guide explains how to use the OpenStore AI skills in `.ai/skills`.

The skills are project-specific workflows. They tell an AI assistant how to build OpenStore features while respecting bounded contexts, tenant isolation, authorization, testing, exceptions, and infrastructure rules.

The skills are plain Markdown workflows. They can be used by Codex, OpenCode, Antigravity CLI, or any other coding agent. If a tool has no native skill support, tell it to read the applicable `.ai/skills/<name>/SKILL.md` file directly.

For tool-specific startup prompts, read `docs/AI_AGENT_PORTABILITY.md`.

For shorter day-to-day prompts, use `.ai/agent-startup/`.

---

## 1. How to invoke a skill

Mention the skill name and the goal in the prompt.

Example:

```text
Use create-feature-slice to implement CreateTenant.
The endpoint must let an authenticated user create a tenant with name and slug.
Validate duplicate slugs, assign TenantId server-side, add typed exceptions, and add unit and integration tests.
```

When using a CLI that does not automatically load project instructions, use a startup routine first:

```text
Follow .ai/agent-startup/codex.md.
Use create-feature-slice to implement CreateTenant.
```

Replace `codex.md` with `opencode.md`, `antigravity.md`, or `generic.md` depending on the tool.

The best prompts include:

- Feature or behavior name.
- Owning bounded context, if known.
- Actor or user role.
- Permission required.
- Tenant and store scope.
- Request and response fields.
- Business rules.
- Required tests.
- Any known external dependency.

If details are missing, the assistant should infer them from `README.md`, `AGENTS.md`, `docs/`, ADRs, and existing code. It should ask only when a business rule or security boundary would otherwise be invented.

---

## 2. Skill selection

Use this table to choose the skill.

| Skill | Use when |
|---|---|
| `create-feature-slice` | Building a complete MVP feature or first vertical slice inside a bounded context. |
| `create-crud-use-case` | Adding one create, read, update, delete, deactivate, archive, or query operation in an existing context. |
| `add-tenant-entity` | Adding or changing an entity that has tenant-owned or store-owned data. |
| `write-tests` | Adding missing coverage, regression tests, tenant isolation tests, or adapter tests. |
| `create-external-service-client` | Integrating an HTTP API, SDK, storage provider, email, maps, payment, shipping, or WhatsApp link behavior. |
| `create-wizard-process` | Building a stateful multi-step workflow such as invitations, onboarding, order preparation, or delivery. |
| `create-microservice` | Creating or extracting an independently deployable service after an ADR-worthy boundary is justified. |
| `update-context-map` | Updating `.ai/CONTEXT_MAP.md` as a token-saving project index. |
| `update-postman-collection` | Adding or updating Postman requests, local environments, response examples, and Newman-ready API functional tests. |
| `manage-feature-branch` | Creating or reusing feature branches and organizing small commits by feature or module. |

Default to `create-feature-slice` for early OpenStore development.

Use `create-microservice` only when the service needs independent deployment, scaling, security, data ownership, or evolution.

---

## 3. Recommended starting prompts

### Create the first tenant feature

```text
Use create-feature-slice to implement CreateTenant in the Tenancy bounded context.
Create the needed src and tests structure if it does not exist yet.
The feature must create a tenant for an authenticated user, assign ownership, validate duplicate slug, use typed exceptions, and include success, validation, duplicate, unauthorized, and persistence tests.
```

### Add products to the catalog

```text
Use create-feature-slice and add-tenant-entity to implement CreateProduct in Catalog.
Product belongs to a tenant and store. The request includes name, description, price, currency code, and publication state.
Do not trust TenantId or StoreId from the request body. Add tenant/store ownership checks and cross-tenant tests.
```

### Add WhatsApp cart validation

```text
Use create-feature-slice and create-external-service-client to implement public WhatsApp cart validation.
The frontend sends product IDs and quantities. The backend resolves current products, validates publication and ownership, calculates totals, and returns a wa.me URL.
Add tests for changed prices, unpublished products, empty cart, invalid phone, URL encoding, and totals.
```

### Improve tests for an existing service

```text
Use write-tests for CreateProductService.
Cover success, invalid input, duplicate slug, store from another tenant, unauthorized user, persistence failure, and cancellation.
```

### Update the project context map

```text
Use update-context-map to refresh .ai/CONTEXT_MAP.md.
Inspect the current structure and add only compact pointers for important projects, entry points, commands, and decisions.
```

### Add functional API requests

```text
Use update-postman-collection for CreateTenant.
Add success, validation, unauthorized, duplicate, and cross-tenant requests when applicable.
Use local environment variables and do not commit real secrets.
```

### Work on a feature branch

```text
Use manage-feature-branch and create-feature-slice for CreateTenant.
Create or reuse feature/tenancy-create-tenant.
Keep models, endpoint, tests, Postman requests, docs, and context map updates on this branch.
Use small coherent commits if committing is requested.
Do not push unless I ask.
```

---

## 4. Expected assistant workflow

For implementation work, the assistant should:

1. Read `.ai/CONTEXT_MAP.md` first.
2. Use the context map to decide which docs, ADRs, skills, source files, and tests must be loaded.
3. Read `README.md`, `AGENTS.md`, task-relevant docs, the selected skill, existing code, and existing tests.
4. Follow `docs/GIT_WORKFLOW.md` when code changes or commits are requested.
5. Identify the owning bounded context.
6. Identify tenant boundary, store boundary, permission, exceptions, external dependencies, and required tests.
7. Implement the smallest complete vertical slice.
8. Keep endpoints thin and business logic outside the API layer.
9. Keep application code dependent on interfaces, not infrastructure details.
10. Reuse existing cohesive code before adding duplicate validation, mapping, tenant checks, permission checks, or integration behavior.
11. Extract repeated logic into focused reusable components when it improves clarity.
12. Add or update tests before claiming completion.
13. Run formatting, build, unit tests, and applicable integration tests.
14. Update `.ai/CONTEXT_MAP.md` when structure, entry points, standard commands, or important decisions changed.
15. Add or update Postman requests when HTTP endpoints are added or changed.
16. Use small coherent commits if committing is requested.
17. Report changed files, decisions, test results, branch/commit status, and remaining risks.

---

## 5. Good prompt checklist

Before asking for an implementation, include as much of this as possible:

- [ ] Desired behavior.
- [ ] Bounded context.
- [ ] Authenticated or public endpoint.
- [ ] Required permission.
- [ ] Tenant scope.
- [ ] Store scope.
- [ ] Request fields.
- [ ] Response fields.
- [ ] Business validation.
- [ ] Duplicate or conflict rules.
- [ ] External providers.
- [ ] Expected tests.
- [ ] Whether to create missing project structure.

---

## 6. Guardrails

The assistant must not:

- Create a microservice just because a new table or endpoint exists.
- Trust `TenantId` or `StoreId` from request bodies.
- Share EF entities or domain projects across bounded contexts.
- Query another service database.
- Put business logic in endpoints.
- Return EF entities from APIs.
- Duplicate business rules, validation, mappings, tenant checks, permission checks, or integration behavior when a cohesive reusable component exists or should be extracted.
- Leave `.ai/CONTEXT_MAP.md` stale after important structure or command changes.
- Add new frameworks without an ADR.
- Leave API endpoints undocumented in Postman while there is no frontend, unless the endpoint is internal and explicitly excluded.
- Mix unrelated modules or features in the same feature branch.
- Commit unrelated working tree changes.
- Push a branch unless explicitly asked.
- Mark work complete without build and applicable tests, unless explicitly blocked.

---

## 7. Suggested development order

For the MVP, use skills in this order:

1. `create-feature-slice`: CreateTenant.
2. `create-feature-slice`: CreateStore.
3. `create-feature-slice` + `add-tenant-entity`: CreateProduct.
4. `create-feature-slice`: PublishStore and PublishProduct.
5. `create-feature-slice`: PublicCatalogRead.
6. `create-feature-slice` + `create-external-service-client`: WhatsAppCartValidation.
7. `write-tests`: strengthen tenant isolation and authorization coverage.
8. `update-context-map`: refresh the map after each meaningful structure or command change.
9. `update-postman-collection`: document and functionally exercise API endpoints.
10. `manage-feature-branch`: keep feature work isolated and commits coherent.
11. `create-microservice`: only if a module now deserves independent deployment.
