using WeatherApp.Application;
using WeatherApp.Application.Features.Status;
using WeatherApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("WeatherWeb");

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

app.Run();
