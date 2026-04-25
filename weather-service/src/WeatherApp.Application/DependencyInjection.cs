using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Features.Status;

namespace WeatherApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IStatusService, StatusService>();

        return services;
    }
}
