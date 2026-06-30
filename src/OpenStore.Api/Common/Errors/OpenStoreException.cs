namespace OpenStore.Api.Common.Errors;

public class OpenStoreException : Exception
{
    public OpenStoreException(string errorCode, int statusCode, string message, Exception? innerException = null) : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public string ErrorCode { get; }

    public int StatusCode { get; }
}
