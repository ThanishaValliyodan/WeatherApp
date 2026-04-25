namespace WeatherApp.Application.Abstractions.Persistence;

public interface IDatabaseHealthCheck
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
