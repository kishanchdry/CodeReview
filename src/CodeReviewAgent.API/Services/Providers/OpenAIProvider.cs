using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CodeReviewAgent.API.Services.Providers;

public class OpenAIProvider : IAIProvider
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public OpenAIProvider(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt)
    {
        var request = new
        {
            model = _config["AI:Model"] ?? "gpt-4o-mini",
            max_tokens = int.Parse(_config["AI:MaxOutputTokens"] ?? "800"),
            temperature = double.Parse(_config["AI:Temperature"] ?? "0.3"),
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["AI:ApiKey"]);
        req.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(req);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }
}
