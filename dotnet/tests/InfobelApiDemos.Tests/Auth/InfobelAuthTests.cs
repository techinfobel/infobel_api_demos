using System.Net;
using System.Text.Json.Nodes;
using InfobelApiDemos.Auth;
using RichardSzalay.MockHttp;

namespace InfobelApiDemos.Tests.Auth;

[Collection("EnvVarTests")]
public class GetRequiredEnvVarTests
{
    [Fact]
    public void ReturnsValue()
    {
        Environment.SetEnvironmentVariable("TEST_VAR_DOTNET", "hello");
        try
        {
            Assert.Equal("hello", InfobelAuth.GetRequiredEnvVar("TEST_VAR_DOTNET"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_VAR_DOTNET", null);
        }
    }

    [Fact]
    public void ThrowsWhenMissing()
    {
        Environment.SetEnvironmentVariable("MISSING_VAR_XYZ", null);
        var ex = Assert.Throws<InfobelAuthError>(() => InfobelAuth.GetRequiredEnvVar("MISSING_VAR_XYZ"));
        Assert.Contains("MISSING_VAR_XYZ", ex.Message);
    }

    [Fact]
    public void ThrowsWhenEmpty()
    {
        Environment.SetEnvironmentVariable("EMPTY_VAR", "");
        try
        {
            var ex = Assert.Throws<InfobelAuthError>(() => InfobelAuth.GetRequiredEnvVar("EMPTY_VAR"));
            Assert.Contains("EMPTY_VAR", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("EMPTY_VAR", null);
        }
    }
}

[Collection("EnvVarTests")]
public class BuildTokenPayloadTests
{
    [Fact]
    public void ReturnsCorrectStructure()
    {
        Environment.SetEnvironmentVariable("INFOBEL_USERNAME", "user1");
        Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", "pass1");
        try
        {
            var payload = InfobelAuth.BuildTokenPayload();
            Assert.Equal("password", payload["grant_type"]);
            Assert.Equal("user1", payload["username"]);
            Assert.Equal("pass1", payload["password"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable("INFOBEL_USERNAME", null);
            Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", null);
        }
    }

    [Fact]
    public void ThrowsWhenPasswordMissing()
    {
        Environment.SetEnvironmentVariable("INFOBEL_USERNAME", "user1");
        Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", "");
        try
        {
            var ex = Assert.Throws<InfobelAuthError>(() => InfobelAuth.BuildTokenPayload());
            Assert.Contains("INFOBEL_PASSWORD", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("INFOBEL_USERNAME", null);
            Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", null);
        }
    }
}

[Collection("EnvVarTests")]
public class GetInfobelTokenAsyncTests
{
    [Fact]
    public async Task BizSearchSuccess()
    {
        Environment.SetEnvironmentVariable("INFOBEL_USERNAME", "u");
        Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", "p");
        try
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, InfobelAuth.BizSearchTokenUrl)
                .Respond("application/json", "{\"access_token\":\"tok123\"}");

            var client = mockHttp.ToHttpClient();
            var result = await InfobelAuth.GetInfobelTokenAsync(ApiType.BizSearch, client);
            Assert.Equal("tok123", result["access_token"]!.GetValue<string>());
        }
        finally
        {
            Environment.SetEnvironmentVariable("INFOBEL_USERNAME", null);
            Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", null);
        }
    }

    [Fact]
    public async Task GetDataSuccess()
    {
        Environment.SetEnvironmentVariable("INFOBEL_USERNAME", "u");
        Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", "p");
        try
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, InfobelAuth.GetDataTokenUrl)
                .Respond("application/json", "{\"access_token\":\"tok456\"}");

            var client = mockHttp.ToHttpClient();
            var result = await InfobelAuth.GetInfobelTokenAsync(ApiType.GetData, client);
            Assert.Equal("tok456", result["access_token"]!.GetValue<string>());
        }
        finally
        {
            Environment.SetEnvironmentVariable("INFOBEL_USERNAME", null);
            Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", null);
        }
    }

    [Fact]
    public async Task HttpErrorThrows()
    {
        Environment.SetEnvironmentVariable("INFOBEL_USERNAME", "u");
        Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", "p");
        try
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, InfobelAuth.BizSearchTokenUrl)
                .Respond(HttpStatusCode.Unauthorized, "application/json", "{\"error\":\"unauthorized\"}");

            var client = mockHttp.ToHttpClient();
            var ex = await Assert.ThrowsAsync<InfobelAuthError>(
                () => InfobelAuth.GetInfobelTokenAsync(ApiType.BizSearch, client));
            Assert.Contains("Failed to obtain", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("INFOBEL_USERNAME", null);
            Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", null);
        }
    }

    [Fact]
    public async Task MissingAccessTokenThrows()
    {
        Environment.SetEnvironmentVariable("INFOBEL_USERNAME", "u");
        Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", "p");
        try
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, InfobelAuth.BizSearchTokenUrl)
                .Respond("application/json", "{\"token_type\":\"bearer\"}");

            var client = mockHttp.ToHttpClient();
            var ex = await Assert.ThrowsAsync<InfobelAuthError>(
                () => InfobelAuth.GetInfobelTokenAsync(ApiType.BizSearch, client));
            Assert.Contains("did not include 'access_token'", ex.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable("INFOBEL_USERNAME", null);
            Environment.SetEnvironmentVariable("INFOBEL_PASSWORD", null);
        }
    }
}
