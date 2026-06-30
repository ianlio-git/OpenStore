namespace OpenStore.Api.Common.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; internal set; }

    public DateTimeOffset CreatedAtUtc { get; internal set; }

    public DateTimeOffset? UpdatedAtUtc { get; internal set; }
}
