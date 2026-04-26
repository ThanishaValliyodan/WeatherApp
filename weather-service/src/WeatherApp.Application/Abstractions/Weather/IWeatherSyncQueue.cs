using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Abstractions.Weather;

public interface IWeatherSyncQueue
{
    ValueTask QueueAsync(QueuedSyncRequest request, CancellationToken cancellationToken);
    ValueTask<QueuedSyncRequest> DequeueAsync(CancellationToken cancellationToken);
}

public sealed record QueuedSyncRequest(Guid RunId, SyncRequest Request);
