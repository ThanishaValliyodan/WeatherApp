using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Abstractions.Clock;
using WeatherApp.Application.Abstractions.Weather;
using WeatherApp.Application.DTOs;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Data;

namespace WeatherApp.Infrastructure.Services;

internal sealed class AlertSubscriptionService(
    WeatherDbContext dbContext,
    IClock clock,
    IWeatherQueryService weatherQueryService) : IAlertSubscriptionService
{
    public async Task<AlertSubscriptionDto> CreateAsync(AlertSubscriptionRequest request, CancellationToken cancellationToken)
    {
        AlertRules.ValidateCreateRequest(request);
        var now = clock.UtcNow;
        var subscription = new AlertSubscription
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            Location = request.Location.Trim(),
            AlertType = AlertRules.NormalizeAlertType(request.AlertType),
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
            .Select(subscription => AlertRules.Evaluate(subscription, currentWeather))
            .Where(alert => alert is not null)
            .Select(alert => alert!)
            .ToList();

        return new AlertEvaluationResponse(location, clock.UtcNow, triggeredAlerts);
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
