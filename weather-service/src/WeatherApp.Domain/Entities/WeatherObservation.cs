namespace WeatherApp.Domain.Entities;

public sealed class WeatherObservation
{
    public Guid Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public string MetricUnit { get; set; } = string.Empty;
    public DateTimeOffset ObservationTimeUtc { get; set; }
    public string Provider { get; set; } = "data.gov.sg";
    public string ProviderDataset { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
