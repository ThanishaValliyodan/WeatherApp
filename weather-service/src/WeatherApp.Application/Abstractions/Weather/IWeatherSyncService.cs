using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Abstractions.Weather;

public interface IWeatherSyncService
{
    Task<SyncRunDto> QueueAsync(SyncRequest request, CancellationToken cancellationToken);
    Task<SyncRunDto?> GetRunAsync(Guid id, CancellationToken cancellationToken);
    Task ExecuteQueuedAsync(Guid runId, SyncRequest request, CancellationToken cancellationToken);
}
