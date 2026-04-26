using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Abstractions.Clock;
using WeatherApp.Application.Abstractions.Weather;
using WeatherApp.Application.DTOs;
using WeatherApp.Application.Services;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Data;
using WeatherApp.Infrastructure.Providers.DataGovSg;

namespace WeatherApp.Infrastructure.Services;

internal sealed class WeatherSyncService(
    WeatherDbContext dbContext,
    DataGovSgClient dataGovSgClient,
    IClock clock) : IWeatherSyncService
{
    public async Task<SyncRunDto> SyncAsync(SyncRequest request, CancellationToken cancellationToken)
    {
        SyncRules.ValidateRequest(request);

        var run = new WeatherSyncRun
        {
            Id = Guid.NewGuid(),
            SyncType = "Manual",
            StartedAtUtc = clock.UtcNow,
            Status = "Running",
            RequestedFromDate = request.From,
            RequestedToDate = request.To
        };

        dbContext.WeatherSyncRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        var currentDate = request.From;
        try
        {
            while (currentDate <= request.To)
            {
                foreach (var dataset in DataGovSgDataset.HistoricalDatasets)
                {
                    run.TotalDatesChecked++;

                    var checkpoint = await dbContext.WeatherSyncCheckpoints.FirstOrDefaultAsync(item =>
                        item.Provider == "data.gov.sg"
                        && item.ProviderDataset == dataset
                        && item.LocationScope == "Singapore"
                        && item.DataDate == currentDate,
                        cancellationToken);

                    if (checkpoint is not null && checkpoint.Status == "Succeeded" && !request.Force)
                    {
                        run.TotalDatesSkipped++;
                        continue;
                    }

                    checkpoint ??= new WeatherSyncCheckpoint
                    {
                        Id = Guid.NewGuid(),
                        ProviderDataset = dataset,
                        LocationScope = "Singapore",
                        DataDate = currentDate
                    };

                    try
                    {
                        var pages = await dataGovSgClient.GetByDateAsync(dataset, currentDate, cancellationToken);
                        var checksumSource = string.Join("", pages.Select(page => page.RootElement.GetRawText()));
                        checkpoint.Status = "Succeeded";
                        checkpoint.LastSyncedAtUtc = clock.UtcNow;
                        checkpoint.RecordsInserted = pages.Count;
                        checkpoint.Checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(checksumSource)));
                        checkpoint.ErrorMessage = null;
                        run.TotalDatesFetched++;
                        run.TotalRecordsInserted += pages.Count;
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                    {
                        checkpoint.Status = "Failed";
                        checkpoint.LastSyncedAtUtc = clock.UtcNow;
                        checkpoint.ErrorMessage = ex.Message;
                    }

                    if (dbContext.Entry(checkpoint).State == EntityState.Detached)
                    {
                        dbContext.WeatherSyncCheckpoints.Add(checkpoint);
                    }

                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                currentDate = currentDate.AddDays(1);
            }

            run.Status = "Succeeded";
            run.CompletedAtUtc = clock.UtcNow;
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.CompletedAtUtc = clock.UtcNow;
            run.ErrorMessage = ex.Message;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(run);
    }

    private static SyncRunDto ToDto(WeatherSyncRun run)
    {
        return new SyncRunDto(
            run.Id,
            run.SyncType,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            run.Status,
            run.RequestedFromDate,
            run.RequestedToDate,
            run.TotalDatesChecked,
            run.TotalDatesSkipped,
            run.TotalDatesFetched,
            run.TotalRecordsInserted,
            run.ErrorMessage);
    }
}
