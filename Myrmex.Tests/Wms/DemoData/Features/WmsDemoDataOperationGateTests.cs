using Myrmex.Modules.Wms.DemoData.Features;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataOperationGateTests
{
    [Fact]
    public void Acquire_WhenLeaseIsHeld_RejectsWithoutWaiting()
    {
        WmsDemoDataOperationGate gate = new();
        using IDisposable lease = gate.Acquire();

        Assert.Throws<WmsDemoDataOperationInProgressException>(() => gate.Acquire());
    }

    [Fact]
    public void Dispose_ReleasesLeaseForEitherOperation()
    {
        WmsDemoDataOperationGate gate = new();
        gate.Acquire().Dispose();

        using IDisposable next = gate.Acquire();
    }
}
