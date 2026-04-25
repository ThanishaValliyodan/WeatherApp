namespace WeatherApp.Domain.Entities;

public sealed class WeatherLocation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string? StationId { get; set; }
    public string? Region { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string SourceDataset { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset LastSeenAtUtc { get; set; }
}
