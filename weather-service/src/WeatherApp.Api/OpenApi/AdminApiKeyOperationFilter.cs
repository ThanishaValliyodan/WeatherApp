using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WeatherApp.Api.OpenApi;

public sealed class AdminApiKeyOperationFilter : IOperationFilter
{
    public const string SchemeName = "AdminApiKey";
    public const string HeaderName = "x-admin-api-key";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = (context.ApiDescription.RelativePath ?? string.Empty).TrimEnd('/');
        if (!path.Equals("api/weather/history/sync", StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith("api/weather/history/sync/runs/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = SchemeName
                    }
                }
            ] = []
        });

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized. Provide a valid x-admin-api-key header."
        });
    }
}
