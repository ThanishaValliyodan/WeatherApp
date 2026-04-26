namespace WeatherApp.Api.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "no-referrer");
            headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
            headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");

            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                headers.TryAdd("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'");
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}
