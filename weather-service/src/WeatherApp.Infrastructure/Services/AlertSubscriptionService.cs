using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Abstractions.Clock;
using WeatherApp.Application.Abstractions.Weather;
using WeatherApp.Application.DTOs;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Data;

namespace WeatherApp.Infrastructure.Services;

internal sealed class AlertSubscriptionService(
    WeatherDbContext dbContext,
    IClock clock,
    IWeatherQueryService weatherQueryService) : IAlertSubscriptionService
{
    private static readonly HashSet<string> AlertTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "HighTemperature",
        "HeavyRain",
        "HighHumidity",
        "StrongWind"
    };

    public async Task<AlertSubscriptionDto> CreateAsync(AlertSubscriptionRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var now = clock.UtcNow;
        var subscription = new AlertSubscription
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            Location = request.Location.Trim(),
            AlertType = NormalizeAlertType(request.AlertType),
            ThresholdValue = request.ThresholdValue,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.AlertSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(subscription);
    }

    public async Task<IReadOnlyList<AlertSubscriptionDto>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.AlertSubscriptions
            .AsNoTracking()
            .Where(subscription => subscription.IsActive)
            .OrderBy(subscription => subscription.Location)
            .ThenBy(subscription => subscription.AlertType)
            .Select(subscription => ToDto(subscription))
            .ToListAsync(cancellationToken);
    }

    public async Task<AlertSubscriptionDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.AlertSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(subscription => subscription.Id == id, cancellationToken);

        return subscription is null ? null : ToDto(subscription);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var subscription = await dbContext.AlertSubscriptions
            .FirstOrDefaultAsync(subscription => subscription.Id == id, cancellationToken);

        if (subscription is null)
        {
            return false;
        }

        subscription.IsActive = false;
        subscription.UpdatedAtUtc = clock.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AlertEvaluationResponse> EvaluateAsync(string location, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("location is required.");
        }

        var subscriptions = await dbContext.AlertSubscriptions
            .AsNoTracking()
            .Where(subscription => subscription.IsActive && subscription.Location == location)
            .ToListAsync(cancellationToken);

        var currentWeather = await weatherQueryService.GetCurrentWeatherAsync(location, null, null, null, cancellationToken);
        var triggeredAlerts = subscriptions
            .Select(subscription => EvaluateSubscription(subscription, currentWeather))
            .Where(alert => alert is not null)
            .Select(alert => alert!)
            .ToList();

        return new AlertEvaluationResponse(location, clock.UtcNow, triggeredAlerts);
    }

    private static TriggeredAlertDto? EvaluateSubscription(
        AlertSubscription subscription,
        CurrentWeatherResponse currentWeather)
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

    private static void Validate(AlertSubscriptionRequest request)
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

        if (!AlertTypes.Contains(request.AlertType))
        {
            throw new ArgumentException("alertType must be one of HighTemperature, HeavyRain, HighHumidity, or StrongWind.");
        }

        if (request.ThresholdValue <= 0)
        {
            throw new ArgumentException("thresholdValue must be greater than zero.");
        }
    }

    private static string NormalizeAlertType(string alertType)
    {
        return AlertTypes.First(type => type.Equals(alertType, StringComparison.OrdinalIgnoreCase));
    }

    private static AlertSubscriptionDto ToDto(AlertSubscription subscription)
    {
        return new AlertSubscriptionDto(
            subscription.Id,
            subscription.Email,
            subscription.Location,
            subscription.AlertType,
            subscription.ThresholdValue,
            subscription.IsActive,
            subscription.CreatedAtUtc,
            subscription.UpdatedAtUtc);
    }
}
