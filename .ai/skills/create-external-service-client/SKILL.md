---
name: create-external-service-client
description: Implement or modify an OpenStore external provider adapter for HTTP APIs, SDKs, file stores, object storage, brokers, geocoding, maps, WhatsApp URL generation, email, payment, shipping, or other outside systems. Use when application code needs a port/interface, infrastructure adapter, options validation, typed client, timeout/resilience, safe logging, typed exceptions, and WireMock or adapter tests.
---

# Create External Service Client

## Objective

Implement a tested adapter for an external HTTP API, SDK, file store, broker, or provider.

## Required inputs

- Provider name.
- Business capability.
- Application interface.
- Authentication method.
- Operations.
- Timeout requirements.
- Retry requirements.
- Idempotency requirements.
- Sensitive data involved.

If the provider is only a pure URL formatter, such as a WhatsApp link generator, still keep it behind an application interface when business code depends on it.

## Steps

1. Define an application-layer interface in the owning context.
2. Define immutable application request and result contracts.
3. Define provider options with no secrets committed.
4. Add startup options validation.
5. Implement an infrastructure adapter.
6. Use typed `HttpClient` for HTTP providers or a dedicated SDK wrapper for SDK providers.
7. Pass `CancellationToken` through every async operation.
8. Add timeout configuration.
9. Add resilience only for safe transient operations.
10. Require idempotency before retrying unsafe writes.
11. Map provider DTOs to application contracts.
12. Add typed provider exceptions that preserve the original exception as inner exception when wrapping failures.
13. Add structured safe logging with no secrets, tokens, credentials, or private payloads.
14. Register the adapter through dependency injection in the infrastructure composition root.
15. Create `<AdapterName>Tests`.
16. Use WireMock.Net for HTTP behavior.
17. Test request, response, timeout, cancellation, provider errors, malformed responses, and secret omission from logs.
18. Run formatting, build, and unit tests. Run integration tests only when explicitly requested.

## Constraints

- No provider SDK type leaves Infrastructure.
- No secret is logged.
- No client is constructed in business code.
- No raw `HttpRequestException` reaches Application.
- No retry for unsafe writes without idempotency.
- One return statement per method.
- No early returns.
- No live provider calls in normal automated tests.
- No provider-specific exception type leaks into Application.

## Required tests

- Correct method and path.
- Correct headers.
- Correct body.
- Success mapping.
- Empty body.
- Invalid body.
- Non-success response.
- Timeout.
- Cancellation.
- Retry behavior when configured.
- Typed exception mapping.
- Secrets and sensitive values are not logged.

## Completion report

Report the application interface, infrastructure adapter, options, registration, exception mapping, tests, commands, and any live-provider behavior intentionally left untested.

## Definition of done

The integration is isolated, configurable, resilient where justified, safely logged, and fully tested without calling the live provider.
