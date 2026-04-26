using System.Net.Mail;
using WeatherApp.Application.DTOs;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Services;

public static class AlertRules
{
    private static readonly string[] SupportedAlertTypes =
    [
        "HighTemperature",
        "HeavyRain",
        "HighHumidity",
        "StrongWind"
    ];

    public static void ValidateCreateRequest(AlertSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("email is required.");
        }

        try
        {
            _ = new MailAddress(request.Email);
        }
        catch (FormatException)
        {
            throw new ArgumentException("email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Location))
        {
            throw new ArgumentException("location is required.");
        }

        if (!SupportedAlertTypes.Contains(request.AlertType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("alertType must be one of HighTemperature, HeavyRain, HighHumidity, or StrongWind.");
        }

        if (request.ThresholdValue <= 0)
        {
            throw new ArgumentException("thresholdValue must be greater than zero.");
        }
    }

    public static string NormalizeAlertType(string alertType)
    {
        return SupportedAlertTypes.First(type => type.Equals(alertType, StringComparison.OrdinalIgnoreCase));
    }

    public static TriggeredAlertDto? Evaluate(AlertSubscription subscription, CurrentWeatherResponse currentWeather)
    {
        var metric = subscription.AlertType switch
        {
            "HighTemperature" => (Value: currentWeather.TemperatureCelsius, Unit: "deg C", Label: "temperature"),
            "HeavyRain" => (Value: currentWeather.RainfallMm, Unit: "mm", Label: "rainfall"),
            "HighHumidity" => (Value: currentWeather.HumidityPercent, Unit: "%", Label: "humidity"),
            "StrongWind" => (Value: currentWeather.WindSpeed, Unit: "km/h", Label: "wind speed"),
            _ => (Value: null, Unit: string.Empty, Label: string.Empty)
        };

        if (metric.Value is null || metric.Value < subscription.ThresholdValue)
        {
            return null;
        }

        return new TriggeredAlertDto(
            subscription.Id,
            subscription.Email,
            subscription.Location,
            subscription.AlertType,
            subscription.ThresholdValue,
            metric.Value.Value,
            metric.Unit,
            $"{metric.Label} is {metric.Value.Value:0.##} {metric.Unit}, meeting threshold {subscription.ThresholdValue:0.##} {metric.Unit}.");
    }
}
