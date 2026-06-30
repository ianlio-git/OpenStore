using OpenStore.Api.Common.Contracts;

namespace OpenStore.Api.Common.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
