namespace InfobelApiDemos.Auth;

using System.Text.Json.Nodes;

public static class InfobelAuth
{
    public const string BizSearchTokenUrl = "https://bizsearch.infobelpro.com/api/token";
    public const string GetDataTokenUrl = "https://getdata.infobelpro.com/api/token";

    public static async Task<JsonObject> GetInfobelTokenAsync(
        ApiType apiType,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        DotNetEnv.Env.Load();

        var tokenUrl = GetTokenUrl(apiType);
        var payload = BuildTokenPayload();

        var client = httpClient ?? new HttpClient();
        var content = new FormUrlEncodedContent(payload);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(tokenUrl, content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InfobelAuthError(
                $"Failed to obtain {apiType} token: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InfobelAuthError(
                $"Failed to obtain {apiType} token: {(int)response.StatusCode} {response.ReasonPhrase} | Response: {body}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonNode.Parse(json)?.AsObject()
            ?? throw new InfobelAuthError($"{apiType} token response was not valid JSON.");

        if (data["access_token"] is null)
        {
            throw new InfobelAuthError(
                $"{apiType} token response did not include 'access_token'.");
        }

        return data;
    }

    internal static string GetRequiredEnvVar(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
        {
            throw new InfobelAuthError(
                $"Environment variable '{name}' is required for Infobel authentication.");
        }
        return value;
    }

    internal static Dictionary<string, string> BuildTokenPayload()
    {
        return new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = GetRequiredEnvVar("INFOBEL_USERNAME"),
            ["password"] = GetRequiredEnvVar("INFOBEL_PASSWORD"),
        };
    }

    internal static string GetTokenUrl(ApiType apiType) => apiType switch
    {
        ApiType.BizSearch => BizSearchTokenUrl,
        ApiType.GetData => GetDataTokenUrl,
        _ => throw new ArgumentOutOfRangeException(nameof(apiType))
    };
}
