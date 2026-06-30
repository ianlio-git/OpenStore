# External Integrations

Every external dependency must be isolated behind an application port and an infrastructure adapter.

---

## 1. Structure

```text
Application
â””â”€â”€ IGeocodingService

Infrastructure
â”œâ”€â”€ GeocodingHttpClient
â”œâ”€â”€ GeocodingOptions
â””â”€â”€ GeocodingResponse

Tests
â””â”€â”€ GeocodingHttpClientTests
```

Application interface:

```csharp
public interface IGeocodingService
{
    Task<GeocodingResult> GeocodeAsync(
        Address address,
        CancellationToken cancellationToken);
}
```

Infrastructure implementation:

```csharp
public sealed class GeocodingHttpClient : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeocodingHttpClient> _logger;

    public GeocodingHttpClient(
        HttpClient httpClient,
        ILogger<GeocodingHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Private methods appear before public methods.

    public async Task<GeocodingResult> GeocodeAsync(
        Address address,
        CancellationToken cancellationToken)
    {
        GeocodingResult result;

        try
        {
            result = await SendRequestAsync(address, cancellationToken);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeocodingServiceTimeoutException();
        }
        catch (HttpRequestException exception)
        {
            throw new GeocodingServiceUnavailableException(exception);
        }

        return result;
    }
}
```

---

## 2. Rules

- Use typed clients.
- Use options classes.
- Validate options at startup.
- Use cancellation.
- Define a timeout.
- Add retries only for transient and idempotent operations.
- Do not retry validation failures.
- Do not retry unsafe writes without idempotency.
- Keep provider DTOs inside Infrastructure.
- Map provider DTOs to application contracts.
- Use typed project exceptions.
- Never log secrets.
- Never return provider SDK types to Application or API.
- Every adapter has a dedicated xUnit test class.

---

## 3. Required tests

Each adapter test class covers:

- Request route.
- HTTP verb.
- Query parameters.
- Headers.
- Body serialization.
- Success mapping.
- No-content or empty response.
- Invalid payload.
- Expected error response.
- Timeout.
- Cancellation.
- Retry behavior when enabled.
- Typed exception.
- Sensitive-data logging protection.

Use WireMock.Net for HTTP adapter tests.

---

## 4. WhatsApp integration

The MVP does not call a WhatsApp API.

It generates a `wa.me` URL from validated data.

Even this behavior should be isolated:

```csharp
public interface IWhatsAppLinkBuilder
{
    WhatsAppLink BuildCartLink(
        string phoneNumber,
        WhatsAppCart cart);
}
```

`WhatsAppLinkBuilder` requires a dedicated test class covering:

- Phone normalization.
- URL encoding.
- Product lines.
- Quantities.
- Total.
- Currency.
- Empty cart.
- Invalid phone.
