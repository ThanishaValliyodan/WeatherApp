using WeatherApp.Application.DTOs;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Entities;
using Xunit;

namespace WeatherApp.Application.Tests.Services;

public sealed class AlertRulesTests
{
    [Fact]
    public void ValidateCreateRequest_AllowsValidRequest()
    {
        var request = new AlertSubscriptionRequest(
            "user@example.com",
            "Ang Mo Kio",
            "HighTemperature",
            32);

        AlertRules.ValidateCreateRequest(request);
    }

    [Theory]
    [InlineData("", "Ang Mo Kio", "HighTemperature", 32, "email is required.")]
    [InlineData("not-an-email", "Ang Mo Kio", "HighTemperature", 32, "email format is invalid.")]
    [InlineData("user@example.com", "", "HighTemperature", 32, "location is required.")]
    [InlineData("user@example.com", "Ang Mo Kio", "PoorAirQuality", 32, "alertType must be one of HighTemperature, HeavyRain, HighHumidity, or StrongWind.")]
    [InlineData("user@example.com", "Ang Mo Kio", "HighTemperature", 0, "thresholdValue must be greater than zero.")]
    public void ValidateCreateRequest_RejectsInvalidRequest(
        string email,
        string location,
        string alertType,
        decimal threshold,
        string expectedMessage)
    {
        var request = new AlertSubscriptionRequest(email, location, alertType, threshold);

        var exception = Assert.Throws<ArgumentException>(() => AlertRules.ValidateCreateRequest(request));
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("hightemperature", "HighTemperature")]
    [InlineData("HEAVYRAIN", "HeavyRain")]
    [InlineData("HighHumidity", "HighHumidity")]
    [InlineData("strongwind", "StrongWind")]
    public void NormalizeAlertType_ReturnsCanonicalName(string input, string expected)
    {
        var actual = AlertRules.NormalizeAlertType(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("HighTemperature", 33, 32, "deg C")]
    [InlineData("HeavyRain", 12, 10, "mm")]
    [InlineData("HighHumidity", 85, 80, "%")]
    [InlineData("StrongWind", 30, 25, "km/h")]
    public void Evaluate_ReturnsTriggeredAlert_WhenMetricMeetsThreshold(
        string alertType,
        decimal actualValue,
        decimal threshold,
        string expectedUnit)
    {
        var subscription = CreateSubscription(alertType, threshold);
        var currentWeather = CreateCurrentWeather(alertType, actualValue);

        var result = AlertRules.Evaluate(subscription, currentWeather);

        Assert.NotNull(result);
        Assert.Equal(subscription.Id, result.SubscriptionId);
        Assert.Equal(alertType, result.AlertType);
        Assert.Equal(threshold, result.ThresholdValue);
        Assert.Equal(actualValue, result.ActualValue);
        Assert.Equal(expectedUnit, result.MetricUnit);
    }

    [Fact]
    public void Evaluate_ReturnsNull_WhenMetricIsBelowThreshold()
    {
        var subscription = CreateSubscription("HighTemperature", 35);
        var currentWeather = CreateCurrentWeather("HighTemperature", 30);

        var result = AlertRules.Evaluate(subscription, currentWeather);

        Assert.Null(result);
    }

    [Fact]
    public void Evaluate_ReturnsNull_WhenRelevantMetricIsMissing()
    {
        var subscription = CreateSubscription("HeavyRain", 10);
        var currentWeather = new CurrentWeatherResponse(
            new WeatherLocationDto("Ang Mo Kio", "ForecastArea", null, "central", 1.375m, 103.839m, "two-hr-forecast"),
            [],
            TemperatureCelsius: 30,
            HumidityPercent: 70,
            RainfallMm: null,
            WindSpeed: 12,
            WindDirectionDegrees: 180,
            Sources: []);

        var result = AlertRules.Evaluate(subscription, currentWeather);

        Assert.Null(result);
    }

    private static AlertSubscription CreateSubscription(string alertType, decimal threshold)
    {
        return new AlertSubscription
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Location = "Ang Mo Kio",
            AlertType = alertType,
            ThresholdValue = threshold,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.Parse("2026-04-26T00:00:00Z"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-04-26T00:00:00Z")
        };
    }

    private static CurrentWeatherResponse CreateCurrentWeather(string alertType, decimal value)
    {
        var temperature = alertType == "HighTemperature" ? value : 30;
        var rainfall = alertType == "HeavyRain" ? value : 0;
        var humidity = alertType == "HighHumidity" ? value : 70;
        var windSpeed = alertType == "StrongWind" ? value : 12;

        return new CurrentWeatherResponse(
            new WeatherLocationDto("Ang Mo Kio", "ForecastArea", null, "central", 1.375m, 103.839m, "two-hr-forecast"),
            [],
            temperature,
            humidity,
            rainfall,
            windSpeed,
            WindDirectionDegrees: 180,
            Sources: []);
    }
}
