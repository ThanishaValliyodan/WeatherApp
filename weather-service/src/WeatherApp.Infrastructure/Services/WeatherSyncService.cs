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
    IClock clock,
    IWeatherSyncQueue syncQueue) : IWeatherSyncService
{
    private const int MaxProviderConcurrency = 4;

    public async Task<SyncRunDto> QueueAsync(SyncRequest request, CancellationToken cancellationToken)
    {
        SyncRules.ValidateRequest(request);

        var run = new WeatherSyncRun
        {
            Id = Guid.NewGuid(),
            SyncType = "Manual",
            StartedAtUtc = clock.UtcNow,
            Status = "Queued",
            RequestedFromDate = request.From,
            RequestedToDate = request.To
        };

        dbContext.WeatherSyncRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        await syncQueue.QueueAsync(new QueuedSyncRequest(run.Id, request), cancellationToken);
        return ToDto(run);
    }

    public async Task<SyncRunDto?> GetRunAsync(Guid id, CancellationToken cancellationToken)
    {
        var run = await dbContext.WeatherSyncRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return run is null ? null : ToDto(run);
    }

    public async Task ExecuteQueuedAsync(Guid runId, SyncRequest request, CancellationToken cancellationToken)
    {
        SyncRules.ValidateRequest(request);

        var run = await dbContext.WeatherSyncRuns
            .FirstOrDefaultAsync(item => item.Id == runId, cancellationToken);

        if (run is null)
        {
            return;
        }

        run.Status = "Running";
        run.ErrorMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var currentDate = request.From;
        try
        {
            while (currentDate <= request.To)
            {
                run.TotalDatesChecked += DataGovSgDataset.HistoricalDatasets.Length;
                var existingCheckpoints = await dbContext.WeatherSyncCheckpoints
                    .Where(item =>
                        item.Provider == "data.gov.sg"
                        && DataGovSgDataset.HistoricalDatasets.Contains(item.ProviderDataset)
                        && item.LocationScope == "Singapore"
                        && item.DataDate == currentDate)
                    .ToDictionaryAsync(item => item.ProviderDataset, StringComparer.OrdinalIgnoreCase, cancellationToken);

                var pending = new List<(string Dataset, WeatherSyncCheckpoint Checkpoint)>();
                foreach (var dataset in DataGovSgDataset.HistoricalDatasets)
                {
                    existingCheckpoints.TryGetValue(dataset, out var checkpoint);
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

                    pending.Add((dataset, checkpoint));
                }

                var results = await FetchDatasetResultsAsync(pending.Select(item => item.Dataset), currentDate, cancellationToken);
                foreach (var (dataset, checkpoint) in pending)
                {
                    var result = results[dataset];
                    checkpoint.Status = result.Succeeded ? "Succeeded" : "Failed";
                    checkpoint.LastSyncedAtUtc = clock.UtcNow;
                    checkpoint.RecordsInserted = result.RecordsInserted;
                    checkpoint.Checksum = result.Checksum;
                    checkpoint.ErrorMessage = result.ErrorMessage;

                    if (result.Succeeded)
                    {
                        run.TotalDatesFetched++;
                        run.TotalRecordsInserted += result.RecordsInserted;
                    }

                    if (dbContext.Entry(checkpoint).State == EntityState.Detached)
                    {
                        dbContext.WeatherSyncCheckpoints.Add(checkpoint);
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
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
    }

    private async Task<Dictionary<string, DatasetSyncResult>> FetchDatasetResultsAsync(
        IEnumerable<string> datasets,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(MaxProviderConcurrency);
        var tasks = datasets.Select(async dataset =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return (Dataset: dataset, Result: await FetchDatasetResultAsync(dataset, date, cancellationToken));
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(item => item.Dataset, item => item.Result, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<DatasetSyncResult> FetchDatasetResultAsync(
        string dataset,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        try
        {
            var pages = await dataGovSgClient.GetByDateAsync(dataset, date, cancellationToken);
            try
            {
                var checksumSource = string.Join("", pages.Select(page => page.RootElement.GetRawText()));
                var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(checksumSource)));
                return new DatasetSyncResult(true, pages.Count, checksum, null);
            }
            finally
            {
                foreach (var page in pages)
                {
                    page.Dispose();
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return new DatasetSyncResult(false, 0, null, ex.Message);
        }
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

    private sealed record DatasetSyncResult(
        bool Succeeded,
        int RecordsInserted,
        string? Checksum,
        string? ErrorMessage);
}
