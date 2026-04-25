namespace WeatherApp.Domain.Entities;

public sealed class WeatherForecast
{
    public Guid Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string? Region { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTimeOffset ForecastTimeUtc { get; set; }
    public DateTimeOffset? ValidFromUtc { get; set; }
    public DateTimeOffset? ValidToUtc { get; set; }
    public string ForecastType { get; set; } = string.Empty;
    public decimal? TemperatureLowCelsius { get; set; }
    public decimal? TemperatureHighCelsius { get; set; }
    public decimal? HumidityLowPercent { get; set; }
    public decimal? HumidityHighPercent { get; set; }
    public decimal? WindSpeedLow { get; set; }
    public decimal? WindSpeedHigh { get; set; }
    public string? WindDirection { get; set; }
    public string? ForecastCode { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Provider { get; set; } = "data.gov.sg";
    public string ProviderDataset { get; set; } = string.Empty;
    public string? RawProviderPayload { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
