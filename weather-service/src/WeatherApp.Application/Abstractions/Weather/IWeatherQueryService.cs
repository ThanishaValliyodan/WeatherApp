using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Abstractions.Weather;

public interface IWeatherQueryService
{
    Task<IReadOnlyList<WeatherLocationDto>> GetLocationsAsync(string? query, CancellationToken cancellationToken);

    Task<CurrentWeatherResponse> GetCurrentWeatherAsync(
        string? location,
        CancellationToken cancellationToken);

    Task<ForecastResponse> GetForecastAsync(
        string type,
        string? location,
        string? region,
        CancellationToken cancellationToken);

    Task<HistoricalWeatherResponse> GetHistoryAsync(
        string? location,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<byte[]> ExportHistoryCsvAsync(
        string location,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);
}
