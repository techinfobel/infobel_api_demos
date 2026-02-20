using System.Net;
using System.Text.Json.Nodes;
using InfobelApiDemos.GetData;
using RichardSzalay.MockHttp;

namespace InfobelApiDemos.Tests.GetData;

public class BuildSearchPayloadTests
{
    [Fact]
    public void HasRequiredKeys()
    {
        var payload = GetDataClient.BuildSearchPayload();
        Assert.Equal(1, payload["dataType"]!.GetValue<int>());
        Assert.Equal(10, payload["pageSize"]!.GetValue<int>());
        Assert.Equal("true", payload["returnFirstPage"]!.GetValue<string>());
    }
}

public class RunSearchAsyncTests
{
    [Fact]
    public async Task Success()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, GetDataClient.SearchUrl)
            .Respond("application/json", "{\"firstPageRecords\":[{\"companyName\":\"Intel\"}]}");

        var client = mockHttp.ToHttpClient();
        var payload = new JsonObject { ["dataType"] = 1 };
        var result = await GetDataClient.RunSearchAsync(client, "tok", payload);
        Assert.Equal("Intel", result["firstPageRecords"]![0]!["companyName"]!.GetValue<string>());
    }

    [Fact]
    public async Task HttpError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, GetDataClient.SearchUrl)
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"error\":\"fail\"}");

        var client = mockHttp.ToHttpClient();
        var payload = new JsonObject { ["dataType"] = 1 };
        var ex = await Assert.ThrowsAsync<GetDataApiError>(
            () => GetDataClient.RunSearchAsync(client, "tok", payload));
        Assert.Contains("GetData search failed", ex.Message);
    }
}
