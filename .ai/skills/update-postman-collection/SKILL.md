---
name: update-postman-collection
description: Create or update OpenStore Postman collections, local environments, request examples, response examples, basic Postman test scripts, and Newman-ready functional testing documentation for API endpoints. Use when a feature adds or changes HTTP endpoints, when there is no frontend and APIs must be exercised manually, or when the user asks for Postman, functional API testing, request documentation, collections, environments, or Newman support.
---

# Update Postman Collection

## Objective

Keep API behavior testable and documented through Postman while OpenStore has no frontend.

Postman functional checks complement unit tests and document manual API behavior while full integration tests are deferred.

## Steps

1. Read `.ai/CONTEXT_MAP.md`.
2. Read `docs/POSTMAN_FUNCTIONAL_TESTING.md`.
3. Inspect the API endpoint, request contract, response contract, auth requirement, tenant boundary, and expected status codes.
4. Create `postman/` only when the first API endpoint exists or the task explicitly requests Postman setup.
5. Add or update `postman/OpenStore.postman_collection.json`.
6. Add or update `postman/environments/local.postman_environment.json`.
7. Add requests under folders by bounded context and feature.
8. Use environment variables for `baseUrl`, tokens, tenant IDs, store IDs, product IDs, and slugs.
9. Add basic Postman tests for expected status code and response shape.
10. Add variable capture only when follow-up requests need it.
11. Add success, validation, unauthorized, forbidden, missing, conflict, and cross-tenant requests when applicable.
12. Add or update `postman/README.md` with how to run the collection manually and with Newman.
13. Update `.ai/CONTEXT_MAP.md` when Postman entry points or standard commands change.

## Constraints

- Do not commit real secrets, tokens, passwords, private URLs, or production credentials.
- Do not put complex business assertions in Postman scripts when they belong in automated tests.
- Do not make Postman the only test coverage for important behavior.
- Keep request names clear and stable.
- Keep collections Newman-friendly.

## Completion report

Report:

- Collection and environment files changed.
- Requests added or updated.
- Variables used.
- Manual run instructions.
- Newman command, when available.
- Any cases intentionally left to automated tests.

