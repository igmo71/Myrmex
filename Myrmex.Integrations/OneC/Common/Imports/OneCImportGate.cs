namespace Myrmex.Integrations.OneC.Common.Imports;

internal sealed class OneCImportGate
{
    internal const string Warehouses = "warehouses";
    internal const string UnitsOfMeasure = "uoms";
    internal const string StockKeepingUnits = "skus";

    private readonly IReadOnlyDictionary<string, SemaphoreSlim> _gates =
        new Dictionary<string, SemaphoreSlim>(StringComparer.Ordinal)
        {
            [Warehouses] = new(1, 1),
            [UnitsOfMeasure] = new(1, 1),
            [StockKeepingUnits] = new(1, 1)
        };

    public IDisposable Acquire(string referenceType)
    {
        SemaphoreSlim gate = GetGate(referenceType);
        if (!gate.Wait(0))
        {
            throw new OneCImportAlreadyInProgressException(referenceType);
        }

        return new Lease(gate);
    }

    public IDisposable? TryAcquire(string referenceType)
    {
        SemaphoreSlim gate = GetGate(referenceType);
        return gate.Wait(0) ? new Lease(gate) : null;
    }

    private SemaphoreSlim GetGate(string referenceType) =>
        _gates.TryGetValue(referenceType, out SemaphoreSlim? gate)
            ? gate
            : throw new ArgumentOutOfRangeException(
                nameof(referenceType),
                referenceType,
                "Unknown 1С import reference type.");

    private sealed class Lease(SemaphoreSlim gate) : IDisposable
    {
        private SemaphoreSlim? _gate = gate;

        public void Dispose() => Interlocked.Exchange(ref _gate, null)?.Release();
    }
}

internal sealed class OneCImportAlreadyInProgressException(string referenceType)
    : Exception("An import of this reference type is already in progress.")
{
    public string ReferenceType { get; } = referenceType;
}
