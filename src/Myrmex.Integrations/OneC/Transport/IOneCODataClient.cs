namespace Myrmex.Integrations.OneC.Transport;

internal interface IOneCODataClient
{
    Task TestConnectionAsync(CancellationToken cancellationToken);
}
