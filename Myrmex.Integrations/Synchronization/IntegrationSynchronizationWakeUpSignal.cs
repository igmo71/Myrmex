using System.Threading.Channels;

namespace Myrmex.Integrations.Synchronization;

internal sealed class IntegrationSynchronizationWakeUpSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<bool> Reader => _channel.Reader;

    public void Signal() => _channel.Writer.TryWrite(true);
}
