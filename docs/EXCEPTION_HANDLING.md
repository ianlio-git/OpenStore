# Exception Handling

OpenStore uses typed exceptions and centralized Problem Details mapping.

Business code must not repeatedly create raw exception messages.

---

## 1. Exception model

```csharp
public class OpenStoreException : Exception
{
    public OpenStoreException(string errorCode, int statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public string ErrorCode { get; }

    public int StatusCode { get; }
}
```

All project exceptions inherit from `OpenStoreException` directly. There are no abstract intermediate layers.

Each exception owns its `StatusCode` (HTTP status) and `ErrorCode` (machine-readable domain string).

---

## 2. Concrete exception owns its message

```csharp
public sealed class ProductNotFoundException : OpenStoreException
{
    private const string Code = "catalog.product_not_found";

    public ProductNotFoundException(Guid productId)
        : base(Code, StatusCodes.Status404NotFound, $"The product '{productId}' was not found.")
    {
        ProductId = productId;
    }

    public Guid ProductId { get; }
}
```

Business code:

```csharp
throw new ProductNotFoundException(productId);
```

Business code must not use:

```csharp
throw new InvalidOperationException(
    $"The product '{productId}' was not found.");
```

---

## 3. Validation exceptions

A focused exception may represent one domain rule.

```csharp
public sealed class InvalidProductPriceException : OpenStoreException
{
    private const string Code = "catalog.invalid_product_price";

    public InvalidProductPriceException(decimal price)
        : base(Code, StatusCodes.Status400BadRequest, "Product price must be greater than or equal to zero.")
    {
        Price = price;
    }

    public decimal Price { get; }
}
```

Do not expose sensitive input values in messages.

---

## 4. Tenant exceptions

```csharp
public sealed class TenantAccessException : OpenStoreException
{
    private const string Code = "security.tenant_access_denied";

    public TenantAccessException()
        : base(Code, StatusCodes.Status403Forbidden, "The current user cannot access this tenant resource.")
    {
    }
}
```

Avoid revealing whether another tenant's resource exists.

---

## 5. External service exceptions

```csharp
public sealed class GeocodingServiceUnavailableException : OpenStoreException
{
    private const string Code = "integration.geocoding_unavailable";

    public GeocodingServiceUnavailableException(Exception innerException)
        : base(Code, StatusCodes.Status503ServiceUnavailable, "The geocoding service is currently unavailable.", innerException)
    {
    }
}
```

Infrastructure catches provider-specific exceptions only when it can:

- Translate them.
- Add safe context.
- Apply compensation.
- Return a typed integration failure.

Always preserve the original exception as `InnerException`.

---

## 6. Global mapping

A single global exception handler maps exceptions to RFC 7807 Problem Details.

Mapping logic:

| `OpenStoreException.StatusCode` | HTTP status |
|---:|---:|
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 409 | Conflict |
| 503 | Service Unavailable |
| any other | 500 Internal Server Error |

Problem Details include:

- `type`
- `title`
- `status`
- `detail`
- `instance`
- project `errorCode`

Non-`OpenStoreException` exceptions map to HTTP 500 with `errorCode: "unknown.error"`.

Do not expose stack traces outside development.

---

## 7. Logging

- Log unexpected exceptions once at the boundary.
- Do not log and rethrow unchanged exceptions in every layer.
- Use structured properties.
- Include correlation and trace identifiers.
- Do not log passwords, tokens, secrets, or unnecessary personal data.
- Expected validation and not-found exceptions normally use lower log levels.

---

## 8. Tests

Every exception class with contextual behavior should have tests when necessary.

The global exception handler requires tests for:

- Status.
- Error code.
- Safe detail.
- Correlation ID.
- No stack trace in production.
- Unknown exception fallback.
