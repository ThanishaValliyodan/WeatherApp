using System.Globalization;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Abstractions.Clock;
using WeatherApp.Application.Abstractions.Weather;
using WeatherApp.Application.DTOs;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Data;
using WeatherApp.Infrastructure.Providers.DataGovSg;

namespace WeatherApp.Infrastructure.Services;

internal sealed class WeatherQueryService(
    WeatherDbContext dbContext,
    DataGovSgClient dataGovSgClient,
    IClock clock) : IWeatherQueryService
{
    private static readonly ConcurrentDictionary<string, CurrentWeatherCacheEntry> CurrentWeatherCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CurrentWeatherCacheDuration = TimeSpan.FromSeconds(45);

    private static readonly WeatherLocationDto[] SeedLocations =
    [
        new("Ang Mo Kio", "ForecastArea", null, "central", 1.375m, 103.839m, DataGovSgDataset.TwoHourForecast),
        new("Bedok", "ForecastArea", null, "east", 1.324m, 103.930m, DataGovSgDataset.TwoHourForecast),
        new("Bishan", "ForecastArea", null, "central", 1.352m, 103.849m, DataGovSgDataset.TwoHourForecast),
        new("Bukit Batok", "ForecastArea", null, "west", 1.350m, 103.749m, DataGovSgDataset.TwoHourForecast),
        new("Bukit Merah", "ForecastArea", null, "south", 1.277m, 103.819m, DataGovSgDataset.TwoHourForecast),
        new("Changi", "ForecastArea", "S24", "east", 1.367m, 103.982m, DataGovSgDataset.AirTemperature),
        new("Clementi", "ForecastArea", null, "west", 1.316m, 103.765m, DataGovSgDataset.TwoHourForecast),
        new("Jurong West", "ForecastArea", null, "west", 1.340m, 103.705m, DataGovSgDataset.TwoHourForecast),
        new("Marina Bay", "ForecastArea", null, "south", 1.283m, 103.860m, DataGovSgDataset.TwoHourForecast),
        new("Pasir Ris", "ForecastArea", null, "east", 1.373m, 103.949m, DataGovSgDataset.TwoHourForecast),
        new("Paya Lebar", "Station", "S06", "east", 1.352m, 103.900m, DataGovSgDataset.AirTemperature),
        new("Pulau Ubin", "Station", "S106", "east", 1.416m, 103.967m, DataGovSgDataset.AirTemperature),
        new("Queenstown", "ForecastArea", null, "south", 1.294m, 103.786m, DataGovSgDataset.TwoHourForecast),
        new("Sembawang", "ForecastArea", null, "north", 1.449m, 103.819m, DataGovSgDataset.TwoHourForecast),
        new("Sentosa", "ForecastArea", null, "south", 1.249m, 103.830m, DataGovSgDataset.TwoHourForecast),
        new("Serangoon", "ForecastArea", null, "central", 1.355m, 103.867m, DataGovSgDataset.TwoHourForecast),
        new("Tampines", "ForecastArea", null, "east", 1.354m, 103.944m, DataGovSgDataset.TwoHourForecast),
        new("Toa Payoh", "ForecastArea", null, "central", 1.334m, 103.856m, DataGovSgDataset.TwoHourForecast),
        new("Woodlands", "ForecastArea", "S104", "north", 1.443m, 103.785m, DataGovSgDataset.AirTemperature),
        new("Yishun", "ForecastArea", null, "north", 1.430m, 103.835m, DataGovSgDataset.TwoHourForecast)
    ];

    public async Task<IReadOnlyList<WeatherLocationDto>> GetLocationsAsync(string? query, CancellationToken cancellationToken)
    {
        List<WeatherLocationDto> stored;
        try
        {
            stored = await dbContext.WeatherLocations
                .AsNoTracking()
                .Where(location => location.IsActive)
                .Select(location => new WeatherLocationDto(
                    location.Name,
                    location.LocationType,
                    location.StationId,
                    location.Region,
                    location.Latitude,
                    location.Longitude,
                    location.SourceDataset))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Microsoft.Data.SqlClient.SqlException)
        {
            stored = [];
        }

        var locations = SeedLocations.Concat(stored)
            .GroupBy(location => $"{location.Name}|{location.LocationType}|{location.StationId}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());

        if (!string.IsNullOrWhiteSpace(query))
        {
            locations = locations.Where(location =>
                location.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (location.StationId?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
                || (location.Region?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return locations.OrderBy(location => location.Name).ToList();
    }

    public async Task<CurrentWeatherResponse> GetCurrentWeatherAsync(
        string? location,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveLocationAsync(location, cancellationToken);
        var cacheKey = BuildCurrentWeatherCacheKey(resolved);
        if (CurrentWeatherCache.TryGetValue(cacheKey, out var cached)
            && clock.UtcNow - cached.CachedAtUtc < CurrentWeatherCacheDuration)
        {
            return cached.Response;
        }

        var datasets = new[]
        {
            (DataGovSgDataset.AirTemperature, "Temperature", "deg C"),
            (DataGovSgDataset.RelativeHumidity, "RelativeHumidity", "%"),
            (DataGovSgDataset.Rainfall, "Rainfall", "mm"),
            (DataGovSgDataset.WindSpeed, "WindSpeed", "knots"),
            (DataGovSgDataset.WindDirection, "WindDirection", "degrees")
        };

        var providerResults = await Task.WhenAll(datasets.Select(dataset =>
            FetchCurrentMetricAsync(resolved, dataset.Item1, dataset.Item2, dataset.Item3, cancellationToken)));

        var successfulProviderResults = providerResults
            .Where(result => result is not null)
            .Select(result => result!)
            .ToList();

        var metrics = successfulProviderResults
            .Where(result => result.Metric is not null)
            .Select(result => result.Metric!)
            .ToList();

        var sources = successfulProviderResults
            .Select(result => new ProviderMetadataDto("data.gov.sg", result.Dataset, clock.UtcNow))
            .ToList();

        foreach (var result in successfulProviderResults.Where(result => result.Metric is not null))
        {
            await UpsertObservationAsync(resolved, result.Metric!, result.Dataset, result.RawPayload, cancellationToken);
        }

        await TrySaveChangesAsync(cancellationToken);

        var response = new CurrentWeatherResponse(
            resolved,
            metrics,
            FindMetric(metrics, "Temperature"),
            FindMetric(metrics, "RelativeHumidity"),
            FindMetric(metrics, "Rainfall"),
            FindMetric(metrics, "WindSpeed"),
            FindMetric(metrics, "WindDirection"),
            sources);

        if (sources.Count > 0)
        {
            CurrentWeatherCache[cacheKey] = new CurrentWeatherCacheEntry(clock.UtcNow, response);
        }

        return response;
    }

    private async Task<CurrentMetricResult?> FetchCurrentMetricAsync(
        WeatherLocationDto resolved,
        string dataset,
        string metricType,
        string defaultUnit,
        CancellationToken cancellationToken)
    {
        try
        {
            using var document = await dataGovSgClient.GetLatestAsync(dataset, cancellationToken);
            var metric = dataset is DataGovSgDataset.Pm25 or DataGovSgDataset.Psi or DataGovSgDataset.Uv
                ? ExtractRegionalMetric(document.RootElement, metricType, defaultUnit, resolved.Region)
                : ExtractStationMetric(document.RootElement, metricType, defaultUnit, resolved.StationId);

            return new CurrentMetricResult(dataset, document.RootElement.GetRawText(), metric);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            // Skip this dataset and keep the rest. Provider rate limits and transient errors should not fail the whole response.
            return null;
        }
    }

    public async Task<ForecastResponse> GetForecastAsync(
        string type,
        string? location,
        string? region,
        CancellationToken cancellationToken)
    {
        var normalizedType = type.Trim().ToLowerInvariant();
        var dataset = normalizedType switch
        {
            "two-hour" => DataGovSgDataset.TwoHourForecast,
            "twenty-four-hour" => DataGovSgDataset.TwentyFourHourForecast,
            "four-day" => DataGovSgDataset.FourDayOutlook,
            _ => throw new ArgumentException("type must be one of two-hour, twenty-four-hour, or four-day.")
        };

        if (normalizedType == "two-hour" && string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("location is required for two-hour forecasts.");
        }

        using var document = await dataGovSgClient.GetLatestAsync(dataset, cancellationToken);
        var items = normalizedType switch
        {
            "two-hour" => ExtractTwoHourForecasts(document.RootElement, location),
            "twenty-four-hour" => ExtractTwentyFourHourForecasts(document.RootElement, region),
            _ => ExtractFourDayForecasts(document.RootElement)
        };

        foreach (var item in items)
        {
            await UpsertForecastAsync(normalizedType, dataset, item, document.RootElement.GetRawText(), cancellationToken);
        }

        await TrySaveChangesAsync(cancellationToken);

        return new ForecastResponse(
            normalizedType,
            location,
            region,
            items,
            [new ProviderMetadataDto("data.gov.sg", dataset, clock.UtcNow)]);
    }

    public async Task<HistoricalWeatherResponse> GetHistoryAsync(
        string? location,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        ValidateHistoryRequest(location, from, to);
        var (fromUtc, toExclusiveUtc) = BuildSingaporeDateRangeUtc(from, to);

        var query = dbContext.WeatherObservations.AsNoTracking()
            .Where(record => record.ObservationTimeUtc >= fromUtc && record.ObservationTimeUtc < toExclusiveUtc);

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(record => record.Location == location);
        }

        List<HistoricalWeatherRecordDto> records;
        try
        {
            records = await query
                .OrderBy(record => record.ObservationTimeUtc)
                .Select(record => new HistoricalWeatherRecordDto(
                    record.Location,
                    record.LocationType,
                    record.StationId,
                    record.Region,
                    record.ObservationTimeUtc,
                    record.MetricType,
                    record.MetricValue,
                    record.MetricUnit,
                    record.Provider,
                    record.ProviderDataset,
                    record.CreatedAtUtc))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Microsoft.Data.SqlClient.SqlException)
        {
            records = [];
        }

        return new HistoricalWeatherResponse(location!, from, to, records);
    }

    public async Task<byte[]> ExportHistoryCsvAsync(string location, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("location is required.");
        }

        var history = await GetHistoryAsync(location, from, to, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("Location,LocationType,StationId,Region,TimestampUtc,MetricType,MetricValue,MetricUnit,Provider,ProviderDataset,CreatedAtUtc");

        foreach (var record in history.Records)
        {
            builder.AppendLine(string.Join(",", [
                Csv(record.Location),
                Csv(record.LocationType),
                Csv(record.StationId),
                Csv(record.Region),
                record.TimestampUtc.ToString("O", CultureInfo.InvariantCulture),
                Csv(record.MetricType),
                record.MetricValue.ToString(CultureInfo.InvariantCulture),
                Csv(record.MetricUnit),
                Csv(record.Provider),
                Csv(record.ProviderDataset),
                record.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)
            ]));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private async Task<WeatherLocationDto> ResolveLocationAsync(
        string? location,
        CancellationToken cancellationToken)
    {
        var locations = await GetLocationsAsync(null, cancellationToken);

        if (!string.IsNullOrWhiteSpace(location))
        {
            var byName = locations.FirstOrDefault(item => string.Equals(item.Name, location, StringComparison.OrdinalIgnoreCase))
                ?? locations.FirstOrDefault(item => item.Name.Contains(location, StringComparison.OrdinalIgnoreCase));
            if (byName is not null)
            {
                return byName;
            }
        }

        throw new ArgumentException("location is required.");
    }

    private static WeatherMetricDto? ExtractStationMetric(JsonElement root, string metricType, string defaultUnit, string? preferredStationId)
    {
        var stations = JsonHelpers.EnumerateArray(root, "data", "stations")
            .Concat(JsonHelpers.EnumerateArray(root, "data", "station_metadata"))
            .ToDictionary(
                station => JsonHelpers.GetString(station, "id") ?? JsonHelpers.GetString(station, "station_id") ?? string.Empty,
                station => station);

        var unit = JsonHelpers.GetString(root, "data", "readingUnit")
            ?? JsonHelpers.GetString(root, "data", "reading_unit")
            ?? defaultUnit;

        foreach (var reading in JsonHelpers.EnumerateArray(root, "data", "readings").Reverse())
        {
            var timestamp = JsonHelpers.GetDateTimeOffset(reading, "timestamp") ?? DateTimeOffset.UtcNow;
            foreach (var value in JsonHelpers.EnumerateArray(reading, "data"))
            {
                var stationId = JsonHelpers.GetString(value, "stationId")
                    ?? JsonHelpers.GetString(value, "station_id")
                    ?? JsonHelpers.GetString(value, "id");

                if (!string.IsNullOrWhiteSpace(preferredStationId)
                    && !string.Equals(preferredStationId, stationId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var metricValue = JsonHelpers.GetDecimal(value, "value");
                if (metricValue is null)
                {
                    continue;
                }

                stations.TryGetValue(stationId ?? string.Empty, out var station);
                return new WeatherMetricDto(
                    metricType,
                    metricValue,
                    unit,
                    timestamp,
                    stationId,
                    station.ValueKind == JsonValueKind.Object ? JsonHelpers.GetString(station, "name") : null);
            }
        }

        return null;
    }

    private static WeatherMetricDto? ExtractRegionalMetric(JsonElement root, string metricType, string defaultUnit, string? region)
    {
        var unit = JsonHelpers.GetString(root, "data", "readingUnit")
            ?? JsonHelpers.GetString(root, "data", "reading_unit")
            ?? defaultUnit;

        foreach (var reading in JsonHelpers.EnumerateArray(root, "data", "readings").Reverse())
        {
            var timestamp = JsonHelpers.GetDateTimeOffset(reading, "timestamp") ?? DateTimeOffset.UtcNow;
            foreach (var property in reading.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var selectedRegion = string.IsNullOrWhiteSpace(region) ? "national" : region;
                var value = JsonHelpers.GetDecimal(property.Value, selectedRegion!)
                    ?? JsonHelpers.GetDecimal(property.Value, "national")
                    ?? property.Value.EnumerateObject().Select(item => JsonHelpers.GetDecimal(property.Value, item.Name)).FirstOrDefault(item => item is not null);

                if (value is not null)
                {
                    return new WeatherMetricDto(metricType, value, unit, timestamp, null, selectedRegion);
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<ForecastItemDto> ExtractTwoHourForecasts(JsonElement root, string? location)
    {
        var metadata = JsonHelpers.EnumerateArray(root, "data", "area_metadata")
            .Concat(JsonHelpers.EnumerateArray(root, "data", "areaMetadata"))
            .ToDictionary(item => JsonHelpers.GetString(item, "name") ?? string.Empty, item => item, StringComparer.OrdinalIgnoreCase);

        var results = new List<ForecastItemDto>();

        foreach (var item in EnumerateForecastRecords(root))
        {
            var validFrom = GetDateTimeOffsetAny(item, ["valid_period", "start"], ["validPeriod", "start"]);
            var validTo = GetDateTimeOffsetAny(item, ["valid_period", "end"], ["validPeriod", "end"]);

            foreach (var forecast in JsonHelpers.EnumerateArray(item, "forecasts"))
            {
                var area = JsonHelpers.GetString(forecast, "area") ?? JsonHelpers.GetString(forecast, "name") ?? "Singapore";
                if (!string.IsNullOrWhiteSpace(location)
                    && !area.Contains(location, StringComparison.OrdinalIgnoreCase)
                    && !location.Contains(area, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                metadata.TryGetValue(area, out var meta);
                results.Add(new ForecastItemDto(
                    area,
                    null,
                    GetDateTimeOffsetAny(item, ["updatedTimestamp"], ["update_timestamp"], ["timestamp"]),
                    validFrom,
                    validTo,
                    GetForecastText(forecast) ?? JsonHelpers.GetString(forecast, "forecast") ?? string.Empty,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    GetForecastCode(forecast) ?? JsonHelpers.GetString(meta, "forecast_code") ?? JsonHelpers.GetString(meta, "forecastCode")));
            }
        }

        return results;
    }

    private static IReadOnlyList<ForecastItemDto> ExtractTwentyFourHourForecasts(JsonElement root, string? region)
    {
        var results = new List<ForecastItemDto>();
        foreach (var item in EnumerateForecastRecords(root))
        {
            var general = JsonHelpers.GetProperty(item, "general");
            if (general is not null)
            {
                var validFrom = GetDateTimeOffsetAny(general.Value, ["validPeriod", "start"], ["valid_period", "start"])
                    ?? GetDateTimeOffsetAny(item, ["validPeriod", "start"], ["valid_period", "start"]);
                var validTo = GetDateTimeOffsetAny(general.Value, ["validPeriod", "end"], ["valid_period", "end"])
                    ?? GetDateTimeOffsetAny(item, ["validPeriod", "end"], ["valid_period", "end"]);

                results.Add(new ForecastItemDto(
                    "Singapore",
                    "national",
                    GetDateTimeOffsetAny(item, ["updatedTimestamp"], ["update_timestamp"], ["timestamp"]),
                    validFrom,
                    validTo,
                    GetForecastText(general.Value) ?? string.Empty,
                    JsonHelpers.GetDecimal(general.Value, "temperature", "low"),
                    JsonHelpers.GetDecimal(general.Value, "temperature", "high"),
                    JsonHelpers.GetDecimal(general.Value, "relativeHumidity", "low") ?? JsonHelpers.GetDecimal(general.Value, "relative_humidity", "low"),
                    JsonHelpers.GetDecimal(general.Value, "relativeHumidity", "high") ?? JsonHelpers.GetDecimal(general.Value, "relative_humidity", "high"),
                    JsonHelpers.GetDecimal(general.Value, "wind", "speed", "low"),
                    JsonHelpers.GetDecimal(general.Value, "wind", "speed", "high"),
                    JsonHelpers.GetString(general.Value, "wind", "direction"),
                    GetForecastCode(general.Value)));
            }

            foreach (var period in JsonHelpers.EnumerateArray(item, "periods"))
            {
                var regions = JsonHelpers.GetProperty(period, "regions");
                if (regions is null || regions.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                foreach (var regionalForecast in regions.Value.EnumerateObject())
                {
                    if (!string.IsNullOrWhiteSpace(region)
                        && !regionalForecast.Name.Equals(region, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    results.Add(new ForecastItemDto(
                        regionalForecast.Name,
                        regionalForecast.Name,
                        GetDateTimeOffsetAny(item, ["updatedTimestamp"], ["update_timestamp"], ["timestamp"]),
                        GetDateTimeOffsetAny(period, ["timePeriod", "start"], ["time", "start"]),
                        GetDateTimeOffsetAny(period, ["timePeriod", "end"], ["time", "end"]),
                        GetForecastText(regionalForecast.Value) ?? string.Empty,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        GetForecastCode(regionalForecast.Value)));
                }
            }
        }

        return results;
    }

    private static IReadOnlyList<ForecastItemDto> ExtractFourDayForecasts(JsonElement root)
    {
        var results = new List<ForecastItemDto>();
        foreach (var item in EnumerateForecastRecords(root))
        {
            foreach (var forecast in JsonHelpers.EnumerateArray(item, "forecasts"))
            {
                results.Add(new ForecastItemDto(
                    "Singapore",
                    "national",
                    GetDateTimeOffsetAny(item, ["updatedTimestamp"], ["update_timestamp"], ["timestamp"]),
                    GetDateTimeOffsetAny(forecast, ["timestamp"], ["date"]),
                    GetDateTimeOffsetAny(forecast, ["timestamp"], ["date"]),
                    GetForecastText(forecast) ?? string.Empty,
                    JsonHelpers.GetDecimal(forecast, "temperature", "low"),
                    JsonHelpers.GetDecimal(forecast, "temperature", "high"),
                    JsonHelpers.GetDecimal(forecast, "relativeHumidity", "low") ?? JsonHelpers.GetDecimal(forecast, "relative_humidity", "low"),
                    JsonHelpers.GetDecimal(forecast, "relativeHumidity", "high") ?? JsonHelpers.GetDecimal(forecast, "relative_humidity", "high"),
                    JsonHelpers.GetDecimal(forecast, "wind", "speed", "low"),
                    JsonHelpers.GetDecimal(forecast, "wind", "speed", "high"),
                    JsonHelpers.GetString(forecast, "wind", "direction"),
                    GetForecastCode(forecast)));
            }
        }

        return results;
    }

    private static IEnumerable<JsonElement> EnumerateForecastRecords(JsonElement root)
    {
        return JsonHelpers.EnumerateArray(root, "data", "items")
            .Concat(JsonHelpers.EnumerateArray(root, "data", "records"));
    }

    private static DateTimeOffset? GetDateTimeOffsetAny(JsonElement element, params string[][] paths)
    {
        foreach (var path in paths)
        {
            var value = JsonHelpers.GetDateTimeOffset(element, path);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetForecastText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        var directText = JsonHelpers.GetString(element, "text") ?? JsonHelpers.GetString(element, "summary");
        if (!string.IsNullOrWhiteSpace(directText))
        {
            return directText;
        }

        var forecast = JsonHelpers.GetProperty(element, "forecast");
        if (forecast is null)
        {
            return null;
        }

        return forecast.Value.ValueKind == JsonValueKind.String
            ? forecast.Value.GetString()
            : JsonHelpers.GetString(forecast.Value, "text") ?? JsonHelpers.GetString(forecast.Value, "summary");
    }

    private static string? GetForecastCode(JsonElement element)
    {
        return JsonHelpers.GetString(element, "forecast_code")
            ?? JsonHelpers.GetString(element, "forecastCode")
            ?? JsonHelpers.GetString(element, "forecast", "code");
    }

    private async Task UpsertObservationAsync(
        WeatherLocationDto location,
        WeatherMetricDto metric,
        string dataset,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        if (metric.MetricValue is null || metric.TimestampUtc is null)
        {
            return;
        }

        try
        {
            var exists = await dbContext.WeatherObservations.AnyAsync(record =>
                record.Provider == "data.gov.sg"
                && record.ProviderDataset == dataset
                && record.Location == location.Name
                && record.StationId == metric.StationId
                && record.MetricType == metric.MetricType
                && record.ObservationTimeUtc == metric.TimestampUtc.Value,
                cancellationToken);

            if (exists)
            {
                return;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Microsoft.Data.SqlClient.SqlException)
        {
            return;
        }

        dbContext.WeatherObservations.Add(new WeatherObservation
        {
            Id = Guid.NewGuid(),
            Location = location.Name,
            LocationType = location.LocationType,
            StationId = metric.StationId ?? location.StationId,
            StationName = metric.StationName,
            Region = location.Region,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            ObservationTimeUtc = metric.TimestampUtc.Value,
            MetricType = metric.MetricType,
            MetricValue = metric.MetricValue.Value,
            MetricUnit = metric.MetricUnit,
            TemperatureCelsius = metric.MetricType == "Temperature" ? metric.MetricValue : null,
            HumidityPercent = metric.MetricType == "RelativeHumidity" ? metric.MetricValue : null,
            RainfallMm = metric.MetricType == "Rainfall" ? metric.MetricValue : null,
            WindSpeed = metric.MetricType == "WindSpeed" ? metric.MetricValue : null,
            WindDirectionDegrees = metric.MetricType == "WindDirection" ? metric.MetricValue : null,
            AirQualityIndex = metric.MetricType is "PM25" or "PSI" ? metric.MetricValue : null,
            ProviderDataset = dataset,
            RawProviderPayload = rawPayload,
            CreatedAtUtc = clock.UtcNow
        });
    }

    private async Task UpsertForecastAsync(
        string forecastType,
        string dataset,
        ForecastItemDto item,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        var forecastTime = item.ForecastTimeUtc ?? item.ValidFromUtc ?? clock.UtcNow;
        try
        {
            var exists = await dbContext.WeatherForecasts.AnyAsync(record =>
                record.Provider == "data.gov.sg"
                && record.ProviderDataset == dataset
                && record.Location == item.Location
                && record.ForecastType == forecastType
                && record.ForecastTimeUtc == forecastTime
                && record.ValidFromUtc == item.ValidFromUtc,
                cancellationToken);

            if (exists)
            {
                return;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Microsoft.Data.SqlClient.SqlException)
        {
            return;
        }

        dbContext.WeatherForecasts.Add(new WeatherForecast
        {
            Id = Guid.NewGuid(),
            Location = item.Location,
            LocationType = forecastType == "two-hour" ? "ForecastArea" : "Region",
            Area = forecastType == "two-hour" ? item.Location : null,
            Region = item.Region,
            ForecastTimeUtc = forecastTime,
            ValidFromUtc = item.ValidFromUtc,
            ValidToUtc = item.ValidToUtc,
            ForecastType = forecastType,
            TemperatureLowCelsius = item.TemperatureLowCelsius,
            TemperatureHighCelsius = item.TemperatureHighCelsius,
            HumidityLowPercent = item.HumidityLowPercent,
            HumidityHighPercent = item.HumidityHighPercent,
            WindSpeedLow = item.WindSpeedLow,
            WindSpeedHigh = item.WindSpeedHigh,
            WindDirection = item.WindDirection,
            ForecastCode = item.ForecastCode,
            Summary = item.Summary,
            ProviderDataset = dataset,
            RawProviderPayload = rawPayload,
            CreatedAtUtc = clock.UtcNow
        });
    }

    private async Task TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException or Microsoft.Data.SqlClient.SqlException)
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    private static decimal? FindMetric(IEnumerable<WeatherMetricDto> metrics, string metricType)
    {
        return metrics.FirstOrDefault(metric => metric.MetricType == metricType)?.MetricValue;
    }

    private static string BuildCurrentWeatherCacheKey(WeatherLocationDto location)
    {
        return $"{location.Name}|{location.StationId}|{location.Region}|{location.Latitude}|{location.Longitude}";
    }

    private static void ValidateHistoryRequest(string? location, DateOnly from, DateOnly to)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("location is required.");
        }

        if (from > to)
        {
            throw new ArgumentException("from must not be later than to.");
        }
    }

    private static (DateTimeOffset FromUtc, DateTimeOffset ToExclusiveUtc) BuildSingaporeDateRangeUtc(DateOnly from, DateOnly to)
    {
        var singaporeTimeZone = GetSingaporeTimeZone();
        var fromSingapore = from.ToDateTime(TimeOnly.MinValue);
        var toExclusiveSingapore = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return (
            ToUtc(fromSingapore, singaporeTimeZone),
            ToUtc(toExclusiveSingapore, singaporeTimeZone));
    }

    private static DateTimeOffset ToUtc(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        var offset = timeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    private static TimeZoneInfo GetSingaporeTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
        }
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private sealed record CurrentMetricResult(
        string Dataset,
        string RawPayload,
        WeatherMetricDto? Metric);

    private sealed record CurrentWeatherCacheEntry(
        DateTimeOffset CachedAtUtc,
        CurrentWeatherResponse Response);
}
