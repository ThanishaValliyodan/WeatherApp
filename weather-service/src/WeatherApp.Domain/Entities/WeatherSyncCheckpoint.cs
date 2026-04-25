namespace WeatherApp.Domain.Entities;

public sealed class WeatherSyncCheckpoint
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = "data.gov.sg";
    public string ProviderDataset { get; set; } = string.Empty;
    public string LocationScope { get; set; } = string.Empty;
    public DateOnly DataDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? LastSyncedAtUtc { get; set; }
    public int RecordsInserted { get; set; }
    public string? Checksum { get; set; }
    public string? ErrorMessage { get; set; }
}
