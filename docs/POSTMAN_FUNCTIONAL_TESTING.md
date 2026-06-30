# Postman Functional Testing

OpenStore does not require a frontend during the early MVP.

Until the frontend exists, Postman is the functional testing surface for HTTP APIs. Every implemented API endpoint should be easy to exercise through a documented Postman request.

---

## 1. Local development workflow

### 1.1 Start PostgreSQL

Ensure PostgreSQL is running on your machine. The default connection string
in `src/OpenStore.Api/appsettings.Development.json` expects:

```text
Host=localhost;Database=openstore;Username=postgres;Password=postgres
```

Create the database if it does not exist:

```sql
CREATE DATABASE openstore;
```

### 1.2 Run the API

```powershell
dotnet run --project src/OpenStore.Api
```

The API starts on `https://localhost:5001`. The database schema is created
automatically via `Database.EnsureCreatedAsync()` during application startup when
`ASPNETCORE_ENVIRONMENT=Development`.

### 1.3 Generate a dev JWT

Run the dev token script from the repository root:

```powershell
scripts/create-dev-token.ps1
```

To use a specific user ID:

```powershell
scripts/create-dev-token.ps1 -UserId "00000000-0000-0000-0000-000000000001"
```

Copy the printed token (a long string starting with `eyJ`). There is no
public API endpoint for token generation; this is a developer-only tool.

### 1.4 Configure Postman

1. Import the collection from `postman/OpenStore.postman_collection.json`.
2. Import the environment from `postman/environments/local.postman_environment.json`.
3. Select the `OpenStore Local` environment.
4. Paste the dev token into the `accessToken` variable.

### 1.5 Run the requests in order

| Request | Expected status |
|---|---|
| Create Tenant - Success | 201 |
| Create Tenant - Duplicate Slug | 409 |
| Create Tenant - Validation Failure | 400 |
| Create Tenant - Unauthorized | 401 |

The Unauthorized request does not include an `Authorization` header, so it
tests that `[Authorize]` rejects unauthenticated requests.

---

## 2. Purpose

Postman collections are used to:

- Manually exercise API behavior during development.
- Document request examples.
- Document response examples.
- Validate authentication and authorization flows.
- Validate tenant and store isolation through real HTTP calls.
- Provide a functional testing path that can later run through Newman in CI.

Postman functional checks complement unit tests and document manual API scenarios while full integration tests are deferred.

---

## 3. Required artifacts

When APIs exist, use this structure:

```text
postman/
  OpenStore.postman_collection.json
  environments/
    local.postman_environment.json
  README.md
```

Do not create Postman files before the first API endpoint exists unless the task is explicitly about preparing API test documentation.

---

## 4. Collection organization

Organize requests by bounded context and feature:

```text
OpenStore
  Tenancy
    Create Tenant
    Get Tenant
  Stores
    Create Store
    Publish Store
  Catalog
    Create Product
    Publish Product
    Public Catalog
  Public
    WhatsApp Cart
```

Each request should include:

- HTTP method.
- Route.
- Required headers.
- Example body.
- Auth requirements.
- Pre-request script only when necessary.
- Basic test assertions.
- Short description.

---

## 5. Environment variables

Use environment variables instead of hardcoded values:

| Variable | Purpose |
|---|---|
| `baseUrl` | API base URL, for example `https://localhost:5001`. |
| `accessToken` | JWT access token for authenticated requests. |
| `tenantPublicId` | Current tenant public ID when needed by routes or assertions. |
| `storeId` | Current store ID when needed by routes or assertions. |
| `productId` | Current product ID for follow-up requests. |
| `storeSlug` | Public store slug. |

Never commit real secrets or production tokens.

---

## 6. Required functional cases

For every endpoint, add requests or tests for:

- Success path.
- Validation failure.
- Unauthorized request.
- Forbidden request when permission matters.
- Missing resource when applicable.
- Conflict or duplicate when applicable.
- Cross-tenant access when tenant-owned data is involved.

For public endpoints, add:

- Published resource.
- Unpublished resource hidden.
- Safe projection only.

---

## 7. Postman test script expectations

Each request should include basic assertions:

```javascript
pm.test("status code is expected", function () {
    pm.response.to.have.status(201);
});

pm.test("response has publicId", function () {
    const body = pm.response.json();
    pm.expect(body.publicId).to.be.a("string");
});
```

Use scripts to save IDs only when follow-up requests need them:

```javascript
const body = pm.response.json();
pm.environment.set("tenantPublicId", body.publicId);
```

Keep scripts simple. Complex behavior belongs in unit-tested services or in explicit future integration tests when requested.

---

## 8. Newman

Postman collections should be Newman-friendly so they can later run in CI.

Expected future command shape:

```text
newman run postman/OpenStore.postman_collection.json -e postman/environments/local.postman_environment.json
```

Do not make normal CI depend on production services or production credentials.

---

## 9. Completion rule

When a feature adds or changes an HTTP endpoint, the task is not complete until:

- OpenAPI metadata is updated.
- Automated tests are added.
- Postman collection request is added or updated.
- Important response examples are documented.
- `.ai/CONTEXT_MAP.md` is updated when new API entry points or standard commands appear.

