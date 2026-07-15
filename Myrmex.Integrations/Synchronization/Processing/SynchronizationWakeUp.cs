using System.Threading.Channels;

namespace Myrmex.Integrations.Synchronization.Processing;

internal sealed class SynchronizationWakeUp
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<bool> Reader => _channel.Reader;

    public void Notify() => _channel.Writer.TryWrite(true);
}
