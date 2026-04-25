using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Features.Status;

public interface IStatusService
{
    Task<ServiceStatusResponse> GetStatusAsync(CancellationToken cancellationToken);
}
