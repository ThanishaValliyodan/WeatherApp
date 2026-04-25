namespace WeatherApp.Application.DTOs;

public sealed record ServiceStatusResponse(
    string Service,
    string Status,
    string Version,
    bool DatabaseAvailable,
    DateTimeOffset ServerTimeUtc);
