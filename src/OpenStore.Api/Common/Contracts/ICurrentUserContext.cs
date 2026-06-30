namespace OpenStore.Api.Common.Contracts;

public interface ICurrentUserContext
{
    Guid GetRequiredUserId();

    bool IsAuthenticated { get; }
}
