using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Abstractions.Clock;
using WeatherApp.Application.Abstractions.Persistence;
using WeatherApp.Infrastructure.Data;
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
            options.UseSqlServer(connectionString));

        services.AddScoped<IDatabaseHealthCheck, EfDatabaseHealthCheck>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
