using System.Net;
using System.Text.Json.Nodes;
using InfobelApiDemos.BizSearch;
using RichardSzalay.MockHttp;

namespace InfobelApiDemos.Tests.BizSearch;

public class BuildSearchPayloadTests
{
    [Fact]
    public void ContainsCompanyName()
    {
        var payload = BizSearchClient.BuildSearchPayload("Nvidia");
        Assert.Equal("Nvidia", payload["BusinessName"]!.GetValue<string>());
    }

    [Fact]
    public void HasRequiredKeys()
    {
        var payload = BizSearchClient.BuildSearchPayload("Test");
        Assert.Equal(1, payload["dataType"]!.GetValue<int>());
        Assert.Equal(3, payload["pageSize"]!.GetValue<int>());
        Assert.Equal("true", payload["returnFirstPage"]!.GetValue<string>());
    }
}

public class SearchAndGetFirstPageAsyncTests
{
    [Fact]
    public async Task Success()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, BizSearchClient.SearchUrl)
            .Respond("application/json", "{\"searchId\":99,\"firstPageRecords\":[{\"companyName\":\"Acme\"}]}");

        var client = mockHttp.ToHttpClient();
        var result = await BizSearchClient.SearchAndGetFirstPageAsync(client, "tok", "Acme");
        Assert.Equal(99, result["searchId"]!.GetValue<int>());
    }

    [Fact]
    public async Task HttpError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Post, BizSearchClient.SearchUrl)
            .Respond(HttpStatusCode.InternalServerError, "application/json", "{\"error\":\"bad\"}");

        var client = mockHttp.ToHttpClient();
        var ex = await Assert.ThrowsAsync<BizSearchApiError>(
            () => BizSearchClient.SearchAndGetFirstPageAsync(client, "tok", "X"));
        Assert.Contains("BizSearch search failed", ex.Message);
    }
}

public class GetSearchPageAsyncTests
{
    [Fact]
    public async Task Success()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BizSearchClient.SearchUrl}/99/records/2")
            .Respond("application/json", "{\"records\":[{\"companyName\":\"B\"}]}");

        var client = mockHttp.ToHttpClient();
        var result = await BizSearchClient.GetSearchPageAsync(client, "tok", 99, 2);
        Assert.Equal("B", result["records"]![0]!["companyName"]!.GetValue<string>());
    }

    [Fact]
    public async Task HttpError()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Get, $"{BizSearchClient.SearchUrl}/99/records/2")
            .Respond(HttpStatusCode.NotFound, "application/json", "{\"error\":\"fail\"}");

        var client = mockHttp.ToHttpClient();
        var ex = await Assert.ThrowsAsync<BizSearchApiError>(
            () => BizSearchClient.GetSearchPageAsync(client, "tok", 99, 2));
        Assert.Contains("get records failed", ex.Message);
    }
}
