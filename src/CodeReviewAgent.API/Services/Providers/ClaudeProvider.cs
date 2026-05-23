using System.Text;
using System.Text.Json;

namespace CodeReviewAgent.API.Services.Providers;

public class ClaudeProvider : IAIProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public ClaudeProvider(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt)
    {
        var request = new
        {
            model = _config["AI:ClaudeModel"] ?? "claude-haiku-4-5-20251001",
            max_tokens = int.Parse(_config["AI:MaxOutputTokens"] ?? "800"),
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        req.Headers.Add("x-api-key", _config["AI:ClaudeApiKey"]);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(req);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}
