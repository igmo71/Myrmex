namespace Myrmex.WebApp.Wms.Api;

public sealed class ApiException(
    int? status,
    string message,
    IReadOnlyDictionary<string, string>? extensions = null) : Exception(message)
{
    public int? Status { get; } = status;

    public IReadOnlyDictionary<string, string> Extensions { get; } = extensions ?? new Dictionary<string, string>();
}
