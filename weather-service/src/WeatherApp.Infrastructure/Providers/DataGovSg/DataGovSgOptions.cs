namespace WeatherApp.Infrastructure.Providers.DataGovSg;

public sealed class DataGovSgOptions
{
    public string BaseUrl { get; set; } = "https://api-open.data.gov.sg/v2/real-time/api/";
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 20;
}
