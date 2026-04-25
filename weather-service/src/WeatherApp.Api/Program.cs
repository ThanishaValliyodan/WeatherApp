using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Application;
using WeatherApp.Application.Abstractions.Weather;
using WeatherApp.Application.DTOs;
using WeatherApp.Application.Features.Status;
using WeatherApp.Infrastructure.Data;
using WeatherApp.Infrastructure;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "WeatherApp weather-service",
        Version = "v1",
        Description = "Weather microservice API backed by data.gov.sg and SQL Server."
    });
});

var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("WeatherWeb", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public-api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 20;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("WeatherWeb");
app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch
    {
        // Keep Swagger and provider-backed endpoints available even when local SQL Server needs setup.
    }
}

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapGet("/api/status", async (
    IStatusService statusService,
    CancellationToken cancellationToken) =>
{
    var status = await statusService.GetStatusAsync(cancellationToken);

    return status.DatabaseAvailable
        ? Results.Ok(status)
        : Results.Json(status, statusCode: StatusCodes.Status503ServiceUnavailable);
})
.WithName("GetStatus")
.WithSummary("Returns service and database connectivity status.")
.WithOpenApi();

var api = app.MapGroup("/api").RequireRateLimiting("public-api");

api.MapGet("/locations", async (
    string? query,
    IWeatherQueryService weatherQueryService,
    CancellationToken cancellationToken) =>
{
    var locations = await weatherQueryService.GetLocationsAsync(query, cancellationToken);
    return Results.Ok(locations);
})
.WithName("GetLocations")
.WithSummary("Returns supported Singapore weather locations.")
.WithOpenApi();

api.MapGet("/weather/current", async (
    string? location,
    string? stationId,
    decimal? latitude,
    decimal? longitude,
    IWeatherQueryService weatherQueryService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var currentWeather = await weatherQueryService.GetCurrentWeatherAsync(location, stationId, latitude, longitude, cancellationToken);
        return Results.Ok(currentWeather);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("GetCurrentWeather")
.WithSummary("Returns current weather for a location, station, or coordinates.")
.WithOpenApi();

api.MapGet("/weather/forecast", async (
    string type,
    string? location,
    string? region,
    IWeatherQueryService weatherQueryService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var forecast = await weatherQueryService.GetForecastAsync(type, location, region, cancellationToken);
        return Results.Ok(forecast);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("GetForecast")
.WithSummary("Returns two-hour, twenty-four-hour, or four-day forecasts.")
.WithOpenApi();

api.MapGet("/weather/history", async (
    string? location,
    string? stationId,
    DateOnly from,
    DateOnly to,
    IWeatherQueryService weatherQueryService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var history = await weatherQueryService.GetHistoryAsync(location, stationId, from, to, cancellationToken);
        return Results.Ok(history);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("GetWeatherHistory")
.WithSummary("Returns stored historical weather records.")
.WithOpenApi();

api.MapGet("/weather/export", async (
    string location,
    DateOnly from,
    DateOnly to,
    IWeatherQueryService weatherQueryService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var csv = await weatherQueryService.ExportHistoryCsvAsync(location, from, to, cancellationToken);
        return Results.File(csv, "text/csv", $"weather-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("ExportWeatherHistory")
.WithSummary("Exports stored historical weather records as CSV.")
.WithOpenApi();

api.MapPost("/weather/history/sync", async (
    HttpRequest httpRequest,
    DateOnly? date,
    DateOnly? from,
    DateOnly? to,
    int? months,
    bool? force,
    IWeatherSyncService weatherSyncService,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminRequest(httpRequest, configuration))
    {
        return Results.Unauthorized();
    }

    try
    {
        var syncRequest = BuildSyncRequest(date, from, to, months, force ?? false);
        var run = await weatherSyncService.SyncAsync(syncRequest, cancellationToken);
        return Results.Ok(run);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex) when (IsDatabaseUnavailable(ex))
    {
        return Results.Json(new { error = "Database is unavailable." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("SyncWeatherHistory")
.WithSummary("Runs a protected data.gov.sg historical sync.")
.WithOpenApi();

app.Run();

static bool IsAdminRequest(HttpRequest request, IConfiguration configuration)
{
    var configuredAdminKey = configuration["AdminApiKey"];
    if (string.IsNullOrWhiteSpace(configuredAdminKey))
    {
        return false;
    }

    var providedAdminKey = request.Headers["x-admin-api-key"].FirstOrDefault()
        ?? request.Query["adminApiKey"].FirstOrDefault();

    return string.Equals(configuredAdminKey, providedAdminKey, StringComparison.Ordinal);
}

static SyncRequest BuildSyncRequest(DateOnly? date, DateOnly? from, DateOnly? to, int? months, bool force)
{
    if (date is not null)
    {
        return new SyncRequest(date.Value, date.Value, force);
    }

    if (from is not null && to is not null)
    {
        return new SyncRequest(from.Value, to.Value, force);
    }

    if (months is not null)
    {
        var todaySingapore = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            DateTimeOffset.UtcNow,
            "Singapore Standard Time").DateTime);
        return new SyncRequest(todaySingapore.AddMonths(-months.Value), todaySingapore, force);
    }

    throw new ArgumentException("Provide date, from plus to, or months.");
}

static bool IsDatabaseUnavailable(Exception exception)
{
    return exception is InvalidOperationException
        || exception.GetBaseException().GetType().FullName == "Microsoft.Data.SqlClient.SqlException";
}
