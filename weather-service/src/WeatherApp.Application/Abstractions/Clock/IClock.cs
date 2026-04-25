namespace WeatherApp.Application.Abstractions.Clock;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
