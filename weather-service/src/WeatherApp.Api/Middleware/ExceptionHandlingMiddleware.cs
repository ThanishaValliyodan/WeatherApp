using System.Diagnostics;
using System.Text.Json;

namespace WeatherApp.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Unhandled exception after the response started for {TraceId}.", context.TraceIdentifier);
                throw;
            }

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            logger.LogError(ex, "Unhandled exception for {Method} {Path}. TraceId: {TraceId}.",
                context.Request.Method,
                context.Request.Path,
                traceId);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                error = "An unexpected error occurred.",
                traceId
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, payload, cancellationToken: context.RequestAborted);
        }
    }
}
