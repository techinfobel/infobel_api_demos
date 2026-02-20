using System.Text.Json.Nodes;
using InfobelApiDemos.Auth;
using InfobelApiDemos.BizSearch;
using InfobelApiDemos.Formatting;

return await RunAsync();

static async Task<int> RunAsync()
{
    JsonObject tokenResponse;
    try
    {
        tokenResponse = await InfobelAuth.GetInfobelTokenAsync(ApiType.BizSearch);
    }
    catch (InfobelAuthError ex)
    {
        Console.WriteLine($"Authentication failed: {ex.Message}");
        return 1;
    }

    var accessToken = tokenResponse["access_token"]!.GetValue<string>();
    var companyName = "Nvidia";

    Console.WriteLine($"--- Searching for '{companyName}' and fetching first page ---");

    JsonObject initialResponse;
    try
    {
        using var httpClient = new HttpClient();
        initialResponse = await BizSearchClient.SearchAndGetFirstPageAsync(httpClient, accessToken, companyName);
    }
    catch (BizSearchApiError ex)
    {
        Console.WriteLine(ex.Message);
        return 1;
    }

    var firstPageRecords = initialResponse["firstPageRecords"]?.AsArray();
    if (firstPageRecords is null || firstPageRecords.Count == 0)
    {
        Console.WriteLine("No records returned in the first page.");
        return 0;
    }

    RecordFormatter.PrintResults(firstPageRecords);

    var searchIdNode = initialResponse["searchId"];
    if (searchIdNode is null)
    {
        Console.WriteLine("No searchId returned, cannot fetch subsequent pages.");
        return 0;
    }

    var searchId = searchIdNode.GetValue<long>();

    using var client = new HttpClient();
    for (var page = 2; page <= 3; page++)
    {
        Console.WriteLine($"--- Fetching page {page} for search ID {searchId} ---");

        JsonObject pagedResponse;
        try
        {
            pagedResponse = await BizSearchClient.GetSearchPageAsync(client, accessToken, searchId, page);
        }
        catch (BizSearchApiError ex)
        {
            Console.WriteLine(ex.Message);
            continue;
        }

        var records = pagedResponse["records"]?.AsArray();
        if (records is null || records.Count == 0)
        {
            Console.WriteLine($"No records returned on page {page}.");
            break;
        }

        RecordFormatter.PrintResults(records);
    }

    return 0;
}
