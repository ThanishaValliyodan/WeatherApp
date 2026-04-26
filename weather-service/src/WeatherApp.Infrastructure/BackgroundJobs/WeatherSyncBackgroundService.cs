using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeatherApp.Application.Abstractions.Weather;

namespace WeatherApp.Infrastructure.BackgroundJobs;

internal sealed class WeatherSyncBackgroundService(
    IWeatherSyncQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<WeatherSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            QueuedSyncRequest workItem;
            try
            {
                workItem = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IWeatherSyncService>();
                await syncService.ExecuteQueuedAsync(workItem.RunId, workItem.Request, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error while executing weather sync run {RunId}.", workItem.RunId);
            }
        }
    }
}
