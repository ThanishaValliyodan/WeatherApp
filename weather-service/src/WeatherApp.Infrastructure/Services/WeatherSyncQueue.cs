using System.Threading.Channels;
using WeatherApp.Application.Abstractions.Weather;

namespace WeatherApp.Infrastructure.Services;

internal sealed class WeatherSyncQueue : IWeatherSyncQueue
{
    private readonly Channel<QueuedSyncRequest> _queue = Channel.CreateUnbounded<QueuedSyncRequest>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public async ValueTask QueueAsync(QueuedSyncRequest request, CancellationToken cancellationToken)
    {
        await _queue.Writer.WriteAsync(request, cancellationToken);
    }

    public async ValueTask<QueuedSyncRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
