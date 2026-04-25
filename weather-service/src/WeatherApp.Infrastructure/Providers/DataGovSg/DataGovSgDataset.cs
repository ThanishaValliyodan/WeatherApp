namespace WeatherApp.Infrastructure.Providers.DataGovSg;

internal static class DataGovSgDataset
{
    public const string AirTemperature = "air-temperature";
    public const string RelativeHumidity = "relative-humidity";
    public const string Rainfall = "rainfall";
    public const string WindSpeed = "wind-speed";
    public const string WindDirection = "wind-direction";
    public const string Pm25 = "pm25";
    public const string Psi = "psi";
    public const string Uv = "uv";
    public const string TwoHourForecast = "two-hr-forecast";
    public const string TwentyFourHourForecast = "twenty-four-hr-forecast";
    public const string FourDayOutlook = "four-day-outlook";

    public static readonly string[] HistoricalDatasets =
    [
        AirTemperature,
        RelativeHumidity,
        Rainfall,
        WindSpeed,
        WindDirection,
        Pm25,
        Psi,
        TwoHourForecast,
        TwentyFourHourForecast,
        FourDayOutlook
    ];
}
