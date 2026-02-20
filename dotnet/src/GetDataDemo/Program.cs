using System.Text.Json.Nodes;
using InfobelApiDemos.Auth;
using InfobelApiDemos.GetData;
using InfobelApiDemos.Formatting;

return await RunAsync();

static async Task<int> RunAsync()
{
    JsonObject tokenResponse;
    try
    {
        tokenResponse = await InfobelAuth.GetInfobelTokenAsync(ApiType.GetData);
    }
    catch (InfobelAuthError ex)
    {
        Console.WriteLine($"Authentication failed: {ex.Message}");
        return 1;
    }

    var accessToken = tokenResponse["access_token"]!.GetValue<string>();
    var payload = GetDataClient.BuildSearchPayload();

    JsonObject searchResponse;
    try
    {
        using var httpClient = new HttpClient();
        searchResponse = await GetDataClient.RunSearchAsync(httpClient, accessToken, payload);
    }
    catch (GetDataApiError ex)
    {
        Console.WriteLine(ex.Message);
        return 1;
    }

    var records = searchResponse["firstPageRecords"]?.AsArray();
    if (records is null || records.Count == 0)
    {
        Console.WriteLine("No records returned in the first page.");
        return 0;
    }

    RecordFormatter.PrintResults(records);

    return 0;
}
