using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WeatherApp.Infrastructure.Providers.DataGovSg;

internal sealed class DataGovSgClient(HttpClient httpClient, IOptions<DataGovSgOptions> options)
{
    private readonly DataGovSgOptions _options = options.Value;

    public async Task<JsonDocument> GetLatestAsync(string dataset, CancellationToken cancellationToken)
    {
        return await GetAsync(dataset, null, null, cancellationToken);
    }

    public async Task<IReadOnlyList<JsonDocument>> GetByDateAsync(
        string dataset,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var pages = new List<JsonDocument>();
        string? token = null;

        do
        {
            var page = await GetAsync(dataset, date, token, cancellationToken);
            token = JsonHelpers.GetString(page.RootElement, "data", "paginationToken")
                ?? JsonHelpers.GetString(page.RootElement, "paginationToken");
            pages.Add(page);
        }
        while (!string.IsNullOrWhiteSpace(token));

        return pages;
    }

    private async Task<JsonDocument> GetAsync(
        string dataset,
        DateOnly? date,
        string? paginationToken,
        CancellationToken cancellationToken)
    {
        var query = new List<string>();
        if (date is not null)
        {
            query.Add($"date={date:yyyy-MM-dd}");
        }

        if (!string.IsNullOrWhiteSpace(paginationToken))
        {
            query.Add($"paginationToken={Uri.EscapeDataString(paginationToken)}");
        }

        var uri = dataset + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == (HttpStatusCode)429)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            using var retry = await httpClient.SendAsync(request.Clone(), cancellationToken);
            body = await retry.Content.ReadAsStringAsync(cancellationToken);
            retry.EnsureSuccessStatusCode();
            return JsonDocument.Parse(body);
        }

        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(body);
    }
}

internal static class HttpRequestMessageExtensions
{
    public static HttpRequestMessage Clone(this HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
