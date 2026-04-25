using WeatherApp.Application.Abstractions.Clock;

namespace WeatherApp.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
