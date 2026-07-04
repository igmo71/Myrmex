namespace Myrmex.Modules.Wms.DemoData.Features;

internal sealed class WmsDemoDataOperationGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public IDisposable Acquire()
    {
        if (!_gate.Wait(0))
        {
            throw new WmsDemoDataOperationInProgressException();
        }

        return new Lease(_gate);
    }

    private sealed class Lease(SemaphoreSlim gate) : IDisposable
    {
        private SemaphoreSlim? _gate = gate;

        public void Dispose() => Interlocked.Exchange(ref _gate, null)?.Release();
    }
}

internal sealed class WmsDemoDataOperationInProgressException()
    : Exception("Another demo data operation is already in progress.");
