using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Abstractions.Weather;

public interface IWeatherSyncService
{
    Task<SyncRunDto> SyncAsync(SyncRequest request, CancellationToken cancellationToken);
}
