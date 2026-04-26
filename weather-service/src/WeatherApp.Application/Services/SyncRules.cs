using WeatherApp.Application.DTOs;

namespace WeatherApp.Application.Services;

public static class SyncRules
{
    public static void ValidateRequest(SyncRequest request)
    {
        if (request.From > request.To)
        {
            throw new ArgumentException("from must not be later than to.");
        }
    }
}
