using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Abstractions.Persistence;

namespace WeatherApp.Infrastructure.Data;

internal sealed class EfDatabaseHealthCheck(WeatherDbContext dbContext) : IDatabaseHealthCheck
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        return CheckConnectionAsync(cancellationToken);
    }

    private async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}
