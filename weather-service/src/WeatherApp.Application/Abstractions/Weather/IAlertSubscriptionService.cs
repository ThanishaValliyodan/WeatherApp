using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Abstractions.Weather;

public interface IAlertSubscriptionService
{
    Task<AlertSubscriptionDto> CreateAsync(AlertSubscriptionRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertSubscriptionDto>> GetActiveAsync(CancellationToken cancellationToken);
    Task<AlertSubscriptionDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<AlertEvaluationResponse> EvaluateAsync(string location, CancellationToken cancellationToken);
}
