using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Myrmex.Modules.Wms.DemoData.Configuration;
using Myrmex.Modules.Wms.DemoData.Features;

namespace Myrmex.Tests.Wms.DemoData.Features;

public sealed class WmsDemoDataDiagnosticsTests
{
    [Fact]
    public async Task ClearRejection_DoesNotLogConfiguredOrSuppliedConfirmation()
    {
        const string configured = "configured-secret";
        const string supplied = "supplied-secret";
        RecordingLogger<ClearWmsDemoData.Handler> logger = new();
        ClearWmsDemoData.Handler handler = new(
            null!,
            new WmsDemoDataOperationGate(),
            Options.Create(new WmsDemoDataOptions
            {
                AllowClear = true,
                ClearConfirmation = configured
            }),
            logger);

        var result = await handler.HandleAsync(
            new ClearWmsDemoData.Command("actor-1", supplied),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        string combined = string.Join(" ", logger.Messages);
        Assert.DoesNotContain(configured, combined, StringComparison.Ordinal);
        Assert.DoesNotContain(supplied, combined, StringComparison.Ordinal);
        Assert.Contains("DemoData.ClearForbidden", combined, StringComparison.Ordinal);
    }
}

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Messages.Add(formatter(state, exception));
}
