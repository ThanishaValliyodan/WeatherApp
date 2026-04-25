using WeatherApp.Application.Abstractions.Clock;
using WeatherApp.Application.Abstractions.Persistence;
using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Features.Status;

internal sealed class StatusService(
    IClock clock,
    IDatabaseHealthCheck databaseHealthCheck) : IStatusService
{
    public async Task<ServiceStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
    {
        var databaseAvailable = await databaseHealthCheck.CanConnectAsync(cancellationToken);

        return new ServiceStatusResponse(
            Service: "WeatherApp weather-service",
            Status: databaseAvailable ? "Healthy" : "Degraded",
            Version: "0.1.0",
            DatabaseAvailable: databaseAvailable,
            ServerTimeUtc: clock.UtcNow);
    }
}
