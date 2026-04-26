using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WeatherApp.Api.OpenApi;

public sealed class UtcTimestampSchemaFilter : ISchemaFilter
{
    private const string UtcDescription =
        "UTC-normalized timestamp returned by weather-service. data.gov.sg weather APIs may provide source timestamps in Singapore time (+08:00); clients should convert this value to their display timezone.";

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties.Count == 0)
        {
            return;
        }

        foreach (var property in schema.Properties)
        {
            if (!property.Key.EndsWith("Utc", StringComparison.Ordinal))
            {
                continue;
            }

            property.Value.Description = string.IsNullOrWhiteSpace(property.Value.Description)
                ? UtcDescription
                : $"{property.Value.Description} {UtcDescription}";

            property.Value.Example ??= new OpenApiString("2026-04-26T04:00:00+00:00");
        }
    }
}
