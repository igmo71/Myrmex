namespace Myrmex.Modules.Wms.DemoData.Features;

internal interface IWmsDemoDataStageHook
{
    Task StageCompletedAsync(
        string operation,
        string stage,
        CancellationToken cancellationToken);
}

internal sealed class NoOpWmsDemoDataStageHook : IWmsDemoDataStageHook
{
    public Task StageCompletedAsync(
        string operation,
        string stage,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
