namespace WeatherApp.Application.DTOs;

public sealed record WeatherLocationDto(
    string Name,
    string LocationType,
    string? StationId,
    string? Region,
    decimal? Latitude,
    decimal? Longitude,
    string SourceDataset);

public sealed record ProviderMetadataDto(
    string Provider,
    string ProviderDataset,
    DateTimeOffset RetrievedAtUtc);

public sealed record WeatherMetricDto(
    string MetricType,
    decimal? MetricValue,
    string MetricUnit,
    DateTimeOffset? TimestampUtc,
    string? StationId,
    string? StationName);

public sealed record CurrentWeatherResponse(
    WeatherLocationDto ResolvedLocation,
    IReadOnlyList<WeatherMetricDto> Metrics,
    decimal? TemperatureCelsius,
    decimal? HumidityPercent,
    decimal? RainfallMm,
    decimal? WindSpeed,
    decimal? WindDirectionDegrees,
    decimal? Pm25,
    decimal? Psi,
    decimal? UvIndex,
    IReadOnlyList<ProviderMetadataDto> Sources);

public sealed record ForecastResponse(
    string ForecastType,
    string? Location,
    string? Region,
    IReadOnlyList<ForecastItemDto> Items,
    IReadOnlyList<ProviderMetadataDto> Sources);

public sealed record ForecastItemDto(
    string Location,
    string? Region,
    DateTimeOffset? ForecastTimeUtc,
    DateTimeOffset? ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    string Summary,
    decimal? TemperatureLowCelsius,
    decimal? TemperatureHighCelsius,
    decimal? HumidityLowPercent,
    decimal? HumidityHighPercent,
    decimal? WindSpeedLow,
    decimal? WindSpeedHigh,
    string? WindDirection,
    string? ForecastCode);

public sealed record HistoricalWeatherResponse(
    string Location,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<HistoricalWeatherRecordDto> Records);

public sealed record HistoricalWeatherRecordDto(
    string Location,
    string LocationType,
    string? StationId,
    string? Region,
    DateTimeOffset TimestampUtc,
    string MetricType,
    decimal MetricValue,
    string MetricUnit,
    string Provider,
    string ProviderDataset,
    DateTimeOffset CreatedAtUtc);

public sealed record SyncRequest(DateOnly From, DateOnly To, bool Force);

public sealed record SyncRunDto(
    Guid Id,
    string SyncType,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string Status,
    DateOnly? RequestedFromDate,
    DateOnly? RequestedToDate,
    int TotalDatesChecked,
    int TotalDatesSkipped,
    int TotalDatesFetched,
    int TotalRecordsInserted,
    string? ErrorMessage);
