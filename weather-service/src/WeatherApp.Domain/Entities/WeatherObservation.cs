namespace WeatherApp.Domain.Entities;

public sealed class WeatherObservation
{
    public Guid Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string? StationId { get; set; }
    public string? StationName { get; set; }
    public string? Region { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string MetricType { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public string MetricUnit { get; set; } = string.Empty;
    public DateTimeOffset ObservationTimeUtc { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public decimal? HumidityPercent { get; set; }
    public decimal? RainfallMm { get; set; }
    public decimal? WindSpeed { get; set; }
    public decimal? WindDirectionDegrees { get; set; }
    public decimal? AirQualityIndex { get; set; }
    public string? Summary { get; set; }
    public string Provider { get; set; } = "data.gov.sg";
    public string ProviderDataset { get; set; } = string.Empty;
    public string? RawProviderPayload { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
