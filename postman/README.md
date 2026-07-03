# Postman Functional Testing

This folder contains the current manual/functional API contract for OpenStore while there is no frontend.

## Files

- `OpenStore.postman_collection.json`: requests and assertions for Tenancy, Stores, Categories, Products, and Cart.
- `environments/local.postman_environment.json`: local variables used by the collection.

## Prerequisites

- PostgreSQL running locally.
- Database `openstore` created.
- .NET 10 SDK.
- PowerShell 5.1+.

Default development connection string:

```text
Host=localhost;Database=openstore;Username=postgres;Password=postgres
```

Edit `src/OpenStore.Api/appsettings.Development.json` if your local PostgreSQL uses different credentials.

## Run The API

From the repository root:

```powershell
dotnet run --project src/OpenStore.Api
```

The API listens on:

```text
https://localhost:5001
http://localhost:5000
```

In Development, EF creates the current schema automatically with `EnsureCreatedAsync()`.

## Authentication

The collection expects a bearer token in the `accessToken` environment variable.

Generate a local development token:

```powershell
scripts/create-dev-token.ps1
```

Optional fixed user id:

```powershell
scripts/create-dev-token.ps1 -UserId "00000000-0000-0000-0000-000000000001"
```

Copy the printed token into Postman:

```text
OpenStore Local -> accessToken
```

Do not commit real tokens or production secrets.

## Environment Variables

The collection uses these variables:

| Variable | Meaning |
|---|---|
| `baseUrl` | Local API URL, usually `https://localhost:5001`. |
| `accessToken` | Dev JWT used as `Authorization: Bearer {{accessToken}}`. |
| `tenantPublicId` | Captured from `Create Tenant - Success`. |
| `storePublicId` | Captured from `Create Store - Success`. |
| `categoryPublicId` | Captured from `Create Category - Success`. |
| `productPublicId` | Captured from `Create Product - Success`. |
| `cartPublicId` | Captured from `Create Cart - Success`. |
| `cartItemPublicId` | Captured from `Add Item - Success`. |

The success create requests update these variables automatically in their Tests scripts. If a later request returns `404`, first check that the expected variable was captured in the selected Postman environment.

## Recommended Manual Run Order

Run the success path first because later requests depend on variables captured by earlier ones.

1. `Tenancy / Create Tenant - Success`
2. `Stores / Create Store - Success`
3. `Stores / Get Store - Success`
4. `Stores / List Stores By Tenant - Success`
5. `Categories / Create Category - Success`
6. `Categories / Get Category - Success`
7. `Categories / List Categories By Store - Success`
8. `Products / Create Product - Success`
9. `Products / Create Product - With Category`
10. `Products / Get Product - Success`
11. `Products / List Products By Store - Success`
12. `Products / List Products By Category - Success`
13. `Cart / Create Cart - Success`
14. `Cart / Add Item - Success`
15. `Cart / Get Cart - With Items`
16. `Cart / Update Item Quantity - Success`
17. `Cart / Remove Item - Success`
18. `Products / Update Product - Success`
19. `Categories / Update Category - Success`
20. `Stores / Update Store - Success`
21. Run delete requests last: cart items, product, category, then store.

Do not run delete requests in the middle of the sequence unless you want the dependent requests to fail.

## Current Request Folders

### Tenancy

- `Create Tenant - Success` creates the tenant and captures `tenantPublicId`.
- `Create Tenant - Validation Failure` verifies model validation.
- `Create Tenant - Duplicate Slug` verifies duplicate slug handling.
- `Create Tenant - Unauthorized` verifies missing-token behavior.

### Stores

- `Create Store - Success` requires `tenantPublicId` and captures `storePublicId`.
- `Get Store - Success`, `List Stores By Tenant - Success`, `Update Store - Success`, and `Delete Store - Success` depend on `storePublicId`.
- Negative requests cover validation, unauthorized, tenant not found, and store not found.

### Categories

- `Create Category - Success` requires `storePublicId` and captures `categoryPublicId`.
- Get/list/update/delete category requests depend on `storePublicId` and `categoryPublicId`.

### Products

- `Create Product - Success` requires `storePublicId` and captures `productPublicId`.
- `Create Product - With Category` also requires `categoryPublicId`.
- Get/list/update/delete product requests depend on `storePublicId` and `productPublicId`.
- `List Products By Category - Success` depends on `categoryPublicId`.

### Cart

- `Create Cart - Success` requires `storePublicId` and captures `cartPublicId`.
- `Add Item - Success` requires `storePublicId` and `cartPublicId`, and captures `cartItemPublicId`.
- `Get Cart - Empty`, `Get Cart - With Items`, `Update Item Quantity - Success`, and `Remove Item - Success` depend on `storePublicId`, `cartPublicId`, and `cartItemPublicId`.

## Resetting Local Data

If repeated runs hit duplicate slug responses, either change request slugs or reset the local PostgreSQL database.

Development currently uses EF `EnsureCreatedAsync()`, not migrations.

Useful local reset SQL while the schema is still disposable:

```sql
DROP TABLE IF EXISTS "Products" CASCADE;
DROP TABLE IF EXISTS "Categories" CASCADE;
DROP TABLE IF EXISTS "Stores" CASCADE;
DROP TABLE IF EXISTS "TenantMemberships" CASCADE;
DROP TABLE IF EXISTS "Tenants" CASCADE;
```

After dropping tables, restart the API so `EnsureCreatedAsync()` creates the schema again.

## Newman

After setting a valid `accessToken` in the environment file, you can run:

```bash
newman run postman/OpenStore.postman_collection.json -e postman/environments/local.postman_environment.json
```

For normal development, the Postman UI is usually easier because the token and captured ids are visible.
