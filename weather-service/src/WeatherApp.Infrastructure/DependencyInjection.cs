using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Abstractions.Clock;
using WeatherApp.Application.Abstractions.Persistence;
using WeatherApp.Application.Abstractions.Weather;
using WeatherApp.Infrastructure.BackgroundJobs;
using WeatherApp.Infrastructure.Data;
using WeatherApp.Infrastructure.Providers.DataGovSg;
using WeatherApp.Infrastructure.Services;
using WeatherApp.Infrastructure.Time;

namespace WeatherApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        services.AddDbContext<WeatherDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            }));

        services.AddScoped<IDatabaseHealthCheck, EfDatabaseHealthCheck>();
        services.AddSingleton<IClock, SystemClock>();
        services.Configure<DataGovSgOptions>(configuration.GetSection("DataGovSg"));
        services.AddHttpClient<DataGovSgClient>((serviceProvider, client) =>
        {
            var dataGovSgOptions = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<DataGovSgOptions>>()
                .Value;

            client.BaseAddress = new Uri(dataGovSgOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(dataGovSgOptions.TimeoutSeconds);
        });

        services.AddScoped<IWeatherQueryService, WeatherQueryService>();
        services.AddScoped<IAlertSubscriptionService, AlertSubscriptionService>();
        services.AddScoped<IWeatherSyncService, WeatherSyncService>();
        services.AddSingleton<IWeatherSyncQueue, WeatherSyncQueue>();
        services.AddHostedService<WeatherSyncBackgroundService>();

        return services;
    }
}
