using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;

namespace ApiTests.Support;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient client = new(new HttpClientHandler { UseCookies = false })
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("DUMMYJSON_BASE_URL") ?? "https://dummyjson.com/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<JsonNode> Send(HttpMethod method, string path, int expectedStatus,
        object? body = null, string? token = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null) request.Content = JsonContent.Create(body);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        // Do not log request/response bodies: authentication responses contain tokens.
        TestContext.Progress.WriteLine($"{method} {path} -> {(int)response.StatusCode}");
        Assert.That((int)response.StatusCode, Is.EqualTo(expectedStatus), $"{method} {path}: unexpected status");
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())
            ?? throw new InvalidOperationException($"Empty JSON response from {path}");
    }

    public void Dispose() => client.Dispose();
}
