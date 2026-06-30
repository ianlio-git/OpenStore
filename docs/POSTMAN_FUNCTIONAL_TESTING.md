# Postman Functional Testing

OpenStore does not require a frontend during the early MVP.

Until the frontend exists, Postman is the functional testing surface for HTTP APIs. Every implemented API endpoint should be easy to exercise through a documented Postman request.

---

## 1. Purpose

Postman collections are used to:

- Manually exercise API behavior during development.
- Document request examples.
- Document response examples.
- Validate authentication and authorization flows.
- Validate tenant and store isolation through real HTTP calls.
- Provide a functional testing path that can later run through Newman in CI.

Postman functional checks complement unit tests and document manual API scenarios while full integration tests are deferred.

---

## 2. Required artifacts

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

## 3. Collection organization

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

## 4. Environment variables

Use environment variables instead of hardcoded values:

| Variable | Purpose |
|---|---|
| `baseUrl` | API base URL, for example `https://localhost:5001`. |
| `accessToken` | JWT access token for authenticated requests. |
| `tenantId` | Current tenant ID when needed by routes or assertions. |
| `storeId` | Current store ID when needed by routes or assertions. |
| `productId` | Current product ID for follow-up requests. |
| `storeSlug` | Public store slug. |

Never commit real secrets or production tokens.

---

## 5. Required functional cases

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

## 6. Postman test script expectations

Each request should include basic assertions:

```javascript
pm.test("status code is expected", function () {
    pm.response.to.have.status(201);
});

pm.test("response has id", function () {
    const body = pm.response.json();
    pm.expect(body.id).to.be.a("string");
});
```

Use scripts to save IDs only when follow-up requests need them:

```javascript
const body = pm.response.json();
pm.environment.set("tenantId", body.id);
```

Keep scripts simple. Complex behavior belongs in unit-tested services or in explicit future integration tests when requested.

---

## 7. Newman

Postman collections should be Newman-friendly so they can later run in CI.

Expected future command shape:

```text
newman run postman/OpenStore.postman_collection.json -e postman/environments/local.postman_environment.json
```

Do not make normal CI depend on production services or production credentials.

---

## 8. Completion rule

When a feature adds or changes an HTTP endpoint, the task is not complete until:

- OpenAPI metadata is updated.
- Automated tests are added.
- Postman collection request is added or updated.
- Important response examples are documented.
- `.ai/CONTEXT_MAP.md` is updated when new API entry points or standard commands appear.

