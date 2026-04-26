namespace WeatherApp.Domain.Entities;

public sealed class AlertSubscription
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public decimal ThresholdValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
