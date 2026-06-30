# Postman Functional Tests

This directory contains Postman collections for exercising OpenStore API endpoints.

## Setup

1. Import the collection and environment into Postman.
2. Select the `OpenStore Local` environment.
3. Set `accessToken` to a valid JWT token.

## Running

Manual: open the collection and run requests in order.

Newman:

```bash
newman run postman/OpenStore.postman_collection.json \
  -e postman/environments/local.postman_environment.json
```

## Requests

- `Tenancy/Create Tenant - Success`: creates a tenant, captures `tenantId`.
- `Tenancy/Create Tenant - Validation Failure`: empty name and slug â†’ 400.
- `Tenancy/Create Tenant - Duplicate Slug`: repeat the success request â†’ 409.
- `Tenancy/Create Tenant - Unauthorized`: no auth header â†’ 401.
