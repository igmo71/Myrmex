namespace Myrmex.Integrations.OneC.Common.Transport;

internal interface IOneCODataTransport
{
    void ValidateConfiguration();

    Task<IReadOnlyList<T>> ReadCollectionAsync<T>(
        string entitySet,
        IEnumerable<KeyValuePair<string, string>> parameters,
        CancellationToken cancellationToken)
        where T : class;
}
