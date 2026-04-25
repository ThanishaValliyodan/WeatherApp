namespace WeatherApp.Domain.Entities;

public sealed class WeatherSyncRun
{
    public Guid Id { get; set; }
    public string SyncType { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? RequestedFromDate { get; set; }
    public DateOnly? RequestedToDate { get; set; }
    public int TotalDatesChecked { get; set; }
    public int TotalDatesSkipped { get; set; }
    public int TotalDatesFetched { get; set; }
    public int TotalRecordsInserted { get; set; }
    public string? ErrorMessage { get; set; }
}
