using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Application;
using WeatherApp.Application.Abstractions.Weather;
using WeatherApp.Application.DTOs;
using WeatherApp.Application.Features.Status;
using WeatherApp.Api.Middleware;
using WeatherApp.Infrastructure.Data;
using WeatherApp.Infrastructure;
using Microsoft.Net.Http.Headers;
using System.Text.Json;
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

var swaggerEnabled = IsSwaggerEnabled(builder.Configuration, builder.Environment);
if (swaggerEnabled)
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "WeatherApp weather-service",
            Version = "v1",
            Description = "Weather microservice API backed by data.gov.sg and SQL Server."
        });
    });
}

var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? [];

allowedOrigins = ResolveAllowedOrigins(builder, allowedOrigins);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WeatherWeb", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Delete)
            .WithHeaders(HeaderNames.ContentType, "x-admin-api-key");
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

    options.AddFixedWindowLimiter("alerts-api", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 5;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddFixedWindowLimiter("admin-sync-api", limiter =>
    {
        limiter.PermitLimit = 3;
        limiter.Window = TimeSpan.FromMinutes(5);
        limiter.QueueLimit = 0;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please try again later."
        }, cancellationToken);
    };
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

var api = app.MapGroup("/api");

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
.RequireRateLimiting("public-api")
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
    catch (Exception ex) when (IsProviderUnavailable(ex))
    {
        return Results.Json(new { error = "Weather provider is temporarily unavailable." }, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("GetCurrentWeather")
.WithSummary("Returns current weather for a location, station, or coordinates.")
.RequireRateLimiting("public-api")
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
    catch (Exception ex) when (IsProviderUnavailable(ex))
    {
        return Results.Json(new { error = "Weather provider is temporarily unavailable." }, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("GetForecast")
.WithSummary("Returns two-hour, twenty-four-hour, or four-day forecasts.")
.RequireRateLimiting("public-api")
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
.RequireRateLimiting("public-api")
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
.RequireRateLimiting("public-api")
.WithOpenApi();

api.MapPost("/alerts/subscriptions", async (
    AlertSubscriptionRequest request,
    IAlertSubscriptionService alertSubscriptionService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var subscription = await alertSubscriptionService.CreateAsync(request, cancellationToken);
        return Results.Created($"/api/alerts/subscriptions/{subscription.Id}", subscription);
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
.WithName("CreateAlertSubscription")
.WithSummary("Creates a stored weather alert subscription.")
.RequireRateLimiting("alerts-api")
.WithOpenApi();

api.MapGet("/alerts/subscriptions", async (
    IAlertSubscriptionService alertSubscriptionService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var subscriptions = await alertSubscriptionService.GetActiveAsync(cancellationToken);
        return Results.Ok(subscriptions);
    }
    catch (Exception ex) when (IsDatabaseUnavailable(ex))
    {
        return Results.Json(new { error = "Database is unavailable." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("GetAlertSubscriptions")
.WithSummary("Returns active weather alert subscriptions.")
.RequireRateLimiting("public-api")
.WithOpenApi();

api.MapGet("/alerts/subscriptions/{id:guid}", async (
    Guid id,
    IAlertSubscriptionService alertSubscriptionService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var subscription = await alertSubscriptionService.GetAsync(id, cancellationToken);
        return subscription is null ? Results.NotFound() : Results.Ok(subscription);
    }
    catch (Exception ex) when (IsDatabaseUnavailable(ex))
    {
        return Results.Json(new { error = "Database is unavailable." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("GetAlertSubscription")
.WithSummary("Returns one weather alert subscription.")
.RequireRateLimiting("public-api")
.WithOpenApi();

api.MapDelete("/alerts/subscriptions/{id:guid}", async (
    Guid id,
    IAlertSubscriptionService alertSubscriptionService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var deactivated = await alertSubscriptionService.DeactivateAsync(id, cancellationToken);
        return deactivated ? Results.NoContent() : Results.NotFound();
    }
    catch (Exception ex) when (IsDatabaseUnavailable(ex))
    {
        return Results.Json(new { error = "Database is unavailable." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("DeleteAlertSubscription")
.WithSummary("Marks a weather alert subscription inactive.")
.RequireRateLimiting("alerts-api")
.WithOpenApi();

api.MapPost("/alerts/evaluate", async (
    string location,
    IAlertSubscriptionService alertSubscriptionService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var evaluation = await alertSubscriptionService.EvaluateAsync(location, cancellationToken);
        return Results.Ok(evaluation);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex) when (IsProviderUnavailable(ex))
    {
        return Results.Json(new { error = "Weather provider is temporarily unavailable." }, statusCode: StatusCodes.Status502BadGateway);
    }
    catch (Exception ex) when (IsDatabaseUnavailable(ex))
    {
        return Results.Json(new { error = "Database is unavailable." }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("EvaluateAlertSubscriptions")
.WithSummary("Evaluates active alert subscriptions against current weather.")
.RequireRateLimiting("alerts-api")
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
.RequireRateLimiting("admin-sync-api")
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

static bool IsProviderUnavailable(Exception exception)
{
    return exception is HttpRequestException or JsonException or TaskCanceledException;
}

static bool IsSwaggerEnabled(IConfiguration configuration, IWebHostEnvironment environment)
{
    return configuration.GetValue<bool?>("Swagger:Enabled") ?? environment.IsDevelopment();
}

static string[] ResolveAllowedOrigins(WebApplicationBuilder builder, string[] configuredOrigins)
{
    var allowedOrigins = configuredOrigins
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment())
    {
        allowedOrigins =
        [
            "http://localhost:5173",
            "http://127.0.0.1:5173"
        ];
    }

    if (allowedOrigins.Length == 0)
    {
        throw new InvalidOperationException("Configure at least one AllowedOrigins entry for CORS.");
    }

    foreach (var origin in allowedOrigins)
    {
        if (origin.Contains('*', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CORS origins must be explicit. Wildcard origins are not allowed.");
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrWhiteSpace(uri.PathAndQuery.Trim('/')))
        {
            throw new InvalidOperationException($"Invalid CORS origin '{origin}'. Use an absolute HTTP or HTTPS origin without a path.");
        }
    }

    return allowedOrigins;
}
