# OpenStore

OpenStore is a free, open-source, mobile-first, multi-tenant commerce platform.

It is designed for individuals and organizations that need to create one or more independent stores, invite collaborators with scoped roles and permissions, publish product catalogs, support local product discovery, and let buyers send a validated cart to the seller through WhatsApp.

> Project status: architecture definition and initial implementation.
>
> Proposed license: AGPL-3.0-or-later.
>
> Code, technical documentation, commit messages, pull requests, tests, and AI instructions must be written in English.

---

## 1. Product vision

OpenStore should allow a user to:

- Create an account.
- Create or join one or more tenants.
- Create multiple stores inside a tenant.
- Invite tenant-level and store-level collaborators.
- Assign administrators, sellers, catalog editors, order preparers, and delivery users.
- Publish products and categories.
- Share a store or product link on social networks.
- Let a buyer build a cart without creating an account.
- Validate the cart on the backend.
- Generate a WhatsApp link containing the current products, quantities, and total.
- Optionally expose products in a public search experience by category and zone.
- Preserve strict data isolation between tenants.

OpenStore is not initially intended to be a centralized price competition platform. Each store keeps its own identity, catalog, team, and public URL. Global discovery is optional.

---

## 2. Core domain concepts

### User

A global identity representing a person.

A user may belong to multiple tenants and stores with different roles.

### Tenant

An isolated customer account, organization, or business inside OpenStore.

A tenant owns:

- Memberships.
- Configuration.
- Plans and subscriptions.
- Billing information.
- Stores.
- Tenant-scoped data.

### Store

A public store or catalog owned by a tenant.

A store owns or references:

- Store memberships.
- Products.
- Categories.
- Location.
- WhatsApp configuration.
- Catalog appearance.
- Operational workflows.

### Tenant membership

Defines a user's role inside a tenant.

Initial roles:

- `Owner`
- `Administrator`
- `BillingManager`
- `Member`

### Store membership

Defines a user's role and permissions inside a specific store.

Initial roles:

- `Administrator`
- `Seller`
- `CatalogEditor`
- `OrderPreparer`
- `DeliveryPerson`
- `Viewer`

A role is a permission template. Authorization must ultimately evaluate permissions.

---

## 3. Conceptual model

```text
User
+-- TenantMemberships
¦   +-- Tenant
¦       +-- Plan
¦       +-- Subscription
¦       +-- TenantSettings
¦       +-- TenantMemberships
¦       +-- Stores
¦           +-- StoreMemberships
¦           +-- Categories
¦           +-- Products
¦           +-- Orders
¦           +-- Commissions
¦           +-- Deliveries
+-- StoreMemberships
```

Example:

```text
User: Ian
+-- Tenant: Lionetti Company
¦   +-- Tenant role: Owner
¦   +-- Store: Ian Clothing
¦   ¦   +-- Store role: Administrator
¦   +-- Store: Ian Footwear
¦       +-- Store role: Administrator
+-- Tenant: Pedro's Business
    +-- Store: South Accessories
        +-- Store role: Seller
```

---

## 4. Approved technology stack

The authoritative technology list is maintained in [`docs/TECHNOLOGY_STACK.md`](docs/TECHNOLOGY_STACK.md).

Summary:

### Backend

- .NET 10
- C#
- ASP.NET Core Controllers (ControllerBase)
- Entity Framework Core
- PostgreSQL through Npgsql for runtime persistence
- Npgsql
- Postman collection as the current API functional contract
- YARP for the public gateway
- RabbitMQ through the official .NET client when asynchronous messaging is justified
- OpenTelemetry
- JWT Bearer authentication (token issuing deferred - see `AGENTS.md` section 2.1)

### Frontend

- Angular
- Mobile-first responsive design
- PWA
- SSR or prerendering for public shareable pages

### Testing

- xUnit.net v3
- NSubstitute
- Unit tests by default
- Controller tests by direct controller instantiation with substituted services
- NSubstitute for mocks/fakes through contracts
- WireMock.Net only for explicit external HTTP adapter tests
- Built-in xUnit assertions by default

### Local development

- .NET Aspire AppHost for local service orchestration
- Docker/Compose only when deployment infrastructure is introduced; not required for the current unit-test workflow

---

## 5. Architecture

OpenStore is organized by business capability and bounded context.

The initial implementation is intentionally simple:

- One root solution: `OpenStore.sln`.
- One API project: `src/OpenStore.Api`.
- One test project: `tests/OpenStore.Api.Tests`.
- Shared reusable code inside `src/OpenStore.Api/Common`.
- Feature code inside folders such as `src/OpenStore.Api/Tenancy`.
- One `AppDbContext` at the beginning.
- Generic Repository plus Unit of Work for repeated persistence code.
- EntityServiceBase for reusable persistence helpers in feature services.

Do not create one API project per module during the MVP.
Do not create separate Domain/Application/Infrastructure projects at the beginning.
Do not create one `DbContext` or repository per module/entity until the generic structure becomes insufficient. Keep EF entity mappings in feature-level `Persistence/` configuration classes.

Target business modules:

```text
Identity
Tenancy
Stores
Catalog
Orders
Delivery
Search
Notifications
```

Not every target module must be created immediately.

A new microservice is allowed only when:

- It owns a clear business capability.
- It has an explicit data boundary.
- It requires independent deployment, scaling, security, or evolution.
- Its responsibilities do not fit an existing bounded context.
- An ADR approves the split.

A microservice or module must never be created only because a new table exists.

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

---

## 6. Data ownership

Each feature area owns its data rules. The first implementation uses one `AppDbContext` for simplicity.

When a feature area later becomes a real module or independently deployable service, it must own its data boundary explicitly.

```text
Identity -> Identity data boundary
Tenancy  -> Tenancy data boundary
Stores   -> Stores data boundary
Catalog  -> Catalog data boundary
Orders   -> Orders data boundary
Delivery -> Delivery data boundary
Search   -> Search data boundary
```

Initial rules:

- Keep tenant-owned behavior tenant-safe.
- Do not trust tenant or store identifiers from request bodies.
- Keep HTTP controllers thin.
- Put business behavior in services.
- Use `IRepository<TEntity>` for repeated CRUD.
- Use `EntityServiceBase<TEntity>` as a base class for feature services with persistence helpers.
- Use `IUnitOfWork` to save changes once per use case.
- Keep `AppDbContext.OnModelCreating` small; feature mappings live in `Persistence/` configuration classes.
- Extract repositories or module projects only when they reduce real complexity.

---

## 7. Multi-tenancy

Every private tenant-owned entity must implement `ITenantEntity`.

```csharp
public interface ITenantEntity
{
    long TenantId { get; }
}

public abstract class BaseEntity
{
    public long Id { get; internal set; }

    public Guid PublicId { get; internal set; }

    public DateTimeOffset CreatedAtUtc { get; internal set; }

    public DateTimeOffset? UpdatedAtUtc { get; internal set; }

    public bool IsActive { get; internal set; } = true;

    public DateTimeOffset? DeletedAtUtc { get; internal set; }

    public void MarkAsDeleted(DateTimeOffset utcNow) { ... }

    public void Restore() { ... }
}
```
Mandatory rules:

- Never trust a `TenantId` received in a request body.
- Resolve the tenant from an authenticated and authorized context.
- Assign `TenantId` on the server.
- Prevent updates to `TenantId`.
- Filter administrative queries by tenant.
- Validate that related resources belong to the same tenant.
- Include a negative cross-tenant test for every tenant-owned feature.
- Public endpoints must use explicit safe projections.
- `IgnoreQueryFilters()` requires an explicit tenant or public visibility predicate in the same query.

---

## 8. Initial MVP

The first vertical slice must support:

1. Register a user.
2. Create a tenant.
3. Create a store.
4. Create a product.
5. Publish the store and product.
6. Read the public catalog.
7. Build a cart.
8. Validate current products and prices on the backend.
9. Generate a WhatsApp cart URL.
10. Prove that another tenant cannot access or modify the product.

The first version does not include:

- Online payments.
- Tax invoicing.
- Automated shipping providers.
- Native mobile applications.
- Event sourcing.
- A specialized search engine.
- Automatic commission settlement.

---

## 9. WhatsApp cart flow

```text
Buyer
  -> opens a public store
  -> adds products to a local cart
  -> changes quantities
  -> sends ProductId and Quantity to the API
  -> API resolves the store
  -> API validates product visibility and ownership
  -> API loads current prices
  -> API calculates the total
  -> API generates the WhatsApp message
  -> API returns the WhatsApp URL
  -> browser opens WhatsApp
```

Request:

```http
POST /api/public/stores/{storeSlug}/whatsapp-cart
Content-Type: application/json
```

```json
{
  "items": [
    {
      "productId": "00000000-0000-0000-0000-000000000001",
      "quantity": 2
    }
  ]
}
```

Response:

```json
{
  "total": 50000,
  "currencyCode": "ARS",
  "whatsAppUrl": "https://wa.me/54911...?text=..."
}
```

The frontend never defines the authoritative total.

---

## 10. Repository structure

```text
OpenStore/
  OpenStore.sln
  Directory.Build.props
  Directory.Packages.props

  src/
    OpenStore.Api/
      OpenStore.Api.csproj
      Program.cs

      Common/
        Contracts/
          IRepository.cs
          IUnitOfWork.cs
          ITenantEntity.cs
          ICurrentUserContext.cs
          IDateTimeProvider.cs
        Auth/
          CurrentUserContext.cs
          JwtSettings.cs
        Entities/
          BaseEntity.cs

        Persistence/
          AppDbContext.cs
          Repository.cs
          UnitOfWork.cs
        Services/
          EntityServiceBase.cs
        Time/
          SystemDateTimeProvider.cs
        Errors/
          GlobalExceptionHandler.cs
          ModelValidationException.cs
          OpenStoreException.cs
          ProblemDetailsBuilder.cs
        Validation/
          RequiredGuidAttribute.cs
          SlugAttribute.cs

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
        Exceptions/
          DuplicateTenantSlugException.cs

      Stores/
        Controllers/
          StoresController.cs
        Services/
          StoreService.cs
        Contracts/
          IStoreService.cs
        Models/
          Store.cs
        Dtos/
          CreateStoreRequest.cs
          StoreResponse.cs
          UpdateStoreRequest.cs
        Exceptions/
          DuplicateStoreSlugException.cs
          StoreNotFoundException.cs
          TenantNotFoundException.cs
          TenantOwnerRequiredException.cs

  tests/
    OpenStore.Api.Tests/
      OpenStore.Api.Tests.csproj
      Common/
        Errors/
          ModelValidationExceptionTests.cs
          ProblemDetailsBuilderTests.cs
        Services/
          EntityServiceBaseTests.cs
        Validation/
          RequiredGuidAttributeTests.cs
          SlugAttributeTests.cs
      Tenancy/
        Controllers/
          TenantsControllerTests.cs
        Services/
          TenantServiceTests.cs
      Stores/
        Controllers/
          StoresControllerTests.cs
        Services/
          StoreServiceTests.cs

  postman/
    OpenStore.postman_collection.json
    environments/
      local.postman_environment.json
  docs/
    adr/
    examples/
  .ai/
    skills/
    checklists/
  AGENTS.md
  OPENCODE.md
  ANTIGRAVITY.md
  .editorconfig
  Directory.Build.props
  README.md
```

Frontend, Aspire, ServiceDefaults, gateway, messaging, deployment folders, extra projects, and module splits are deferred until the backend API has working features that need them.

---

## 11. Mandatory engineering documentation

- [`AGENTS.md`](AGENTS.md): global AI coding instructions.
- [`docs/TECHNOLOGY_STACK.md`](docs/TECHNOLOGY_STACK.md): approved technologies.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): service boundaries and dependencies.
- [`docs/PROJECT_STRUCTURE.md`](docs/PROJECT_STRUCTURE.md): repository map and where new files belong.
- [`docs/INFRASTRUCTURE.md`](docs/INFRASTRUCTURE.md): local, deployment, configuration, secrets, observability, and CI standards.
- [`docs/GIT_WORKFLOW.md`](docs/GIT_WORKFLOW.md): feature branches, small commits, and PR readiness.
- [`docs/AI_AGENT_PORTABILITY.md`](docs/AI_AGENT_PORTABILITY.md): using the same rules with Codex, OpenCode, Antigravity CLI, or other agents.
- [`docs/CODING_STANDARDS.md`](docs/CODING_STANDARDS.md): C# conventions.
- [`docs/TESTING_STRATEGY.md`](docs/TESTING_STRATEGY.md): required automated tests.
- [`docs/POSTMAN_FUNCTIONAL_TESTING.md`](docs/POSTMAN_FUNCTIONAL_TESTING.md): Postman collections and Newman-ready functional API testing while no frontend exists.
- [`docs/EXCEPTION_HANDLING.md`](docs/EXCEPTION_HANDLING.md): typed exceptions and Problem Details.
- [`docs/EXTERNAL_INTEGRATIONS.md`](docs/EXTERNAL_INTEGRATIONS.md): external service adapter rules.
- [`docs/examples/CODE_EXAMPLES.md`](docs/examples/CODE_EXAMPLES.md): approved implementation examples.
- [`.ai/README.md`](.ai/README.md): guide for using project AI skills.
- [`.ai/skills/`](.ai/skills): repeatable AI workflows.

---

## 12. AI-assisted development

AI tools are not trained by this repository. They are constrained and guided by:

```text
README.md
AGENTS.md
OPENCODE.md
ANTIGRAVITY.md
docs/
.ai/README.md
.ai/CONTEXT_MAP.md
.ai/agent-startup/
.ai/skills/
.ai/checklists/
.editorconfig
Directory.Build.props
tests/
postman/
architecture tests
CI checks
```

Before editing code, an AI assistant must read:

1. `README.md`
2. `AGENTS.md`
3. `.ai/CONTEXT_MAP.md`
4. `.ai/README.md`
5. The applicable `SKILL.md`
6. Related ADRs
7. Existing code and tests in the target service

The assistant must not claim completion until the applicable build and tests pass.

The assistant must update `.ai/CONTEXT_MAP.md` when a task changes important structure, entry points, standard commands, or architecture decisions.

---

## 13. Code reuse

OpenStore favors deliberate code reuse to avoid repeated logic.

Reusable code should be extracted when:

- The same business rule appears in more than one place.
- The same validation rule appears in more than one use case.
- The same mapping, normalization, permission check, tenant check, or external-provider behavior is repeated.
- A shared abstraction makes the code easier to test and understand.

Reusable code must remain cohesive and easy to find.

Do not create broad `Helper`, `Manager`, `Utils`, or shared domain packages just to remove a few lines of duplication. Prefer focused validation attributes, value objects, mappers, policies, options, adapters, small application or domain services, generic repositories, EntityServiceBase, Unit of Work, and reusable repository base classes when persistence code is repeated.

Generic repository reuse is allowed when it removes repeated CRUD plumbing. Repository methods should not call `SaveChangesAsync`; commit through `IUnitOfWork` so one use case can save several changes atomically.

`EntityServiceBase<TEntity>` reuse is allowed for repeated persistence helpers. Do not replace a feature-specific service, such as `TenantService`, when the operation has business rules.

Use one `AppDbContext`. Keep entity mappings in feature-level `Persistence/` configuration classes and register them through `ModelBuilderConfigurationExtensions`.

---

## 14. Definition of done

A change is complete only when:

- The solution builds.
- Applicable tests pass.
- Required new tests exist.
- Postman requests are added or updated for changed HTTP endpoints while no frontend exists.
- Tenant isolation is verified.
- Authorization is verified.
- External calls are abstracted and tested.
- Exceptions follow the approved hierarchy.
- Public contracts, Postman requests, and response examples are updated.
- Documentation is updated.
- No secrets are committed.
- Feature work is kept on the correct feature branch when Git workflow is used.
- Commits are small and coherent when commits are requested.
- No unexplained warnings are introduced.
- The feature boundary remains understandable.
- Repeated logic is extracted into cohesive reusable code when it improves clarity.
- The code follows the single-exit method rule.
- The code follows the approved member ordering.
