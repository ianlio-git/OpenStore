namespace OpenStore.Api.Common.Contracts;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
