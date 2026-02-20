namespace InfobelApiDemos.GetData;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

public static class GetDataClient
{
    public const string SearchUrl = "https://getdata.infobelpro.com/api/search";

    public static JsonObject BuildSearchPayload()
    {
        return new JsonObject
        {
            ["dataType"] = 1,
            ["pageSize"] = 10,
            ["displayLanguage"] = "EN",
            ["returnFirstPage"] = "true",
            ["SortingOrder"] = new JsonArray(5),
            ["CountryCodes"] = new JsonArray("US"),
            ["InternationalCodes"] = new JsonArray("3674"),
        };
    }

    public static async Task<JsonObject> RunSearchAsync(
        HttpClient httpClient,
        string accessToken,
        JsonObject payload,
        CancellationToken cancellationToken = default)
    {
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
            throw new GetDataApiError($"GetData search failed: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GetDataApiError(
                $"GetData search failed: {(int)response.StatusCode} {response.ReasonPhrase} | Response: {body}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonNode.Parse(json)?.AsObject()
            ?? throw new GetDataApiError("GetData search returned invalid JSON.");
    }
}
