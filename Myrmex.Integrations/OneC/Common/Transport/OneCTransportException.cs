namespace Myrmex.Integrations.OneC.Common.Transport;

internal enum OneCTransportFailureReason
{
    Disabled,
    InvalidConfiguration,
    AuthenticationFailed,
    SourceUnavailable,
    EntitySetUnavailable,
    MalformedResponse,
    Timeout
}

internal sealed class OneCTransportException : Exception
{
    public OneCTransportException(
        OneCTransportFailureReason reason,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public OneCTransportFailureReason Reason { get; }
}
