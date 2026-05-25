namespace Myrmex.WebApp.Wms.Topology;

public sealed class ApiException : Exception
{
    public ApiException(
        int? status,
        string message,
        IReadOnlyDictionary<string, string>? extensions = null)
        : base(message)
    {
        Status = status;
        Extensions = extensions ?? new Dictionary<string, string>();
    }

    public int? Status { get; }

    public IReadOnlyDictionary<string, string> Extensions { get; }
}
