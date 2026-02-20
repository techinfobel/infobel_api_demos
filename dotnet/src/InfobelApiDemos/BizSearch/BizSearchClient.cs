namespace InfobelApiDemos.BizSearch;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

public static class BizSearchClient
{
    public const string SearchUrl = "https://bizsearch.infobelpro.com/api/search";

    public static JsonObject BuildSearchPayload(string companyName)
    {
        return new JsonObject
        {
            ["dataType"] = 1,
            ["pageSize"] = 3,
            ["displayLanguage"] = "EN",
            ["returnFirstPage"] = "true",
            ["SortingOrder"] = new JsonArray(5),
            ["BusinessName"] = companyName,
            ["CountryCodes"] = new JsonArray("US"),
        };
    }

    public static async Task<JsonObject> SearchAndGetFirstPageAsync(
        HttpClient httpClient,
        string accessToken,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildSearchPayload(companyName);
        var request = new HttpRequestMessage(HttpMethod.Post, SearchUrl)
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new BizSearchApiError($"BizSearch search failed: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BizSearchApiError(
                $"BizSearch search failed: {(int)response.StatusCode} {response.ReasonPhrase} | Response: {body}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonNode.Parse(json)?.AsObject()
            ?? throw new BizSearchApiError("BizSearch search returned invalid JSON.");
    }

    public static async Task<JsonObject> GetSearchPageAsync(
        HttpClient httpClient,
        string accessToken,
        long searchId,
        int page,
        CancellationToken cancellationToken = default)
    {
        var url = $"{SearchUrl}/{searchId}/records/{page}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new BizSearchApiError($"BizSearch get records failed: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BizSearchApiError(
                $"BizSearch get records failed: {(int)response.StatusCode} {response.ReasonPhrase} | Response: {body}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonNode.Parse(json)?.AsObject()
            ?? throw new BizSearchApiError("BizSearch get records returned invalid JSON.");
    }
}
