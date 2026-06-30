# Postman Functional Tests

This directory contains Postman collections for exercising OpenStore API endpoints.

## Local testing

### Prerequisites

- PostgreSQL running locally (default: `localhost:5432`, database `openstore`)
- .NET 10 SDK
- PowerShell 5.1+

### Setup

1. **Start PostgreSQL** and create the database:

   ```sql
   CREATE DATABASE openstore;
   ```

   Or use an existing database. Edit the connection string in
   `src/OpenStore.Api/appsettings.Development.json` if your PostgreSQL
   uses different credentials.

2. **Run the API**:

   ```powershell
   dotnet run --project src/OpenStore.Api
   ```

   The API starts on `https://localhost:5001`. The database schema is
   created automatically on first run (Development mode).

3. **Generate a dev JWT**:

   ```powershell
   scripts/create-dev-token.ps1
   ```

   To use a specific user ID:

   ```powershell
   scripts/create-dev-token.ps1 -UserId "00000000-0000-0000-0000-000000000001"
   ```

   Copy the printed token.

4. **Import into Postman**:

   - Import `OpenStore.postman_collection.json` as a collection.
   - Import `environments/local.postman_environment.json` as an environment.
   - Select the `OpenStore Local` environment.
   - Set `accessToken` to the token from step 3.

5. **Run requests** in order:

   - `Create Tenant - Success` -> 201
   - `Create Tenant - Duplicate Slug` -> 409 (same slug)
   - `Create Tenant - Validation Failure` -> 400 (empty name/slug)
   - `Create Tenant - Unauthorized` -> 401 (remove `accessToken`)

## Running with Newman

```bash
newman run postman/OpenStore.postman_collection.json \
  -e postman/environments/local.postman_environment.json
```

Requires a valid `accessToken` in the environment.
