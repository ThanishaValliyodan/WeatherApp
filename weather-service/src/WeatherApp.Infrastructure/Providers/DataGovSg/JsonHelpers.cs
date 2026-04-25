using System.Globalization;
using System.Text.Json;

namespace WeatherApp.Infrastructure.Providers.DataGovSg;

internal static class JsonHelpers
{
    public static JsonElement? GetProperty(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var part in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
            {
                return null;
            }
        }

        return current;
    }

    public static string? GetString(JsonElement element, params string[] path)
    {
        var value = GetProperty(element, path);
        return value is { ValueKind: JsonValueKind.String } ? value.Value.GetString() : null;
    }

    public static decimal? GetDecimal(JsonElement element, params string[] path)
    {
        var value = GetProperty(element, path);
        if (value is null)
        {
            return null;
        }

        return value.Value.ValueKind switch
        {
            JsonValueKind.Number when value.Value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.Value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }

    public static DateTimeOffset? GetDateTimeOffset(JsonElement element, params string[] path)
    {
        var value = GetString(element, path);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp)
            ? timestamp.ToUniversalTime()
            : null;
    }

    public static IEnumerable<JsonElement> EnumerateArray(JsonElement element, params string[] path)
    {
        var value = GetProperty(element, path);
        return value is { ValueKind: JsonValueKind.Array } ? value.Value.EnumerateArray() : [];
    }
}
