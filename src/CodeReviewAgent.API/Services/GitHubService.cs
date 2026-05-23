using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeReviewAgent.API.Configuration;
using CodeReviewAgent.API.Models.Git;
using CodeReviewAgent.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.API.Services;

public class GitHubService : IGitService
{
    private readonly HttpClient _http;
    private readonly GitSettings _settings;

    public GitHubService(HttpClient http, IOptions<GitSettings> settings)
    {
        _settings = settings.Value;
        _http = http;
        _http.BaseAddress = new Uri(_settings.BaseUrl);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
        _http.DefaultRequestHeaders.Add("User-Agent", "CodeReviewAgent/1.0");
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<List<FileDiff>> GetPullRequestDiffAsync(string owner, string repo, int prNumber)
    {
        var response = await _http.GetAsync($"/repos/{owner}/{repo}/pulls/{prNumber}/files");
        response.EnsureSuccessStatusCode();

        var files = await response.Content.ReadFromJsonAsync<List<GitHubFile>>() ?? [];
        return files.Select(f => new FileDiff
        {
            FileName = f.Filename,
            Patch = f.Patch ?? string.Empty,
            Status = f.Status,
            Additions = f.Additions,
            Deletions = f.Deletions
        }).ToList();
    }

    public async Task PostReviewCommentsAsync(string owner, string repo, int prNumber, List<ReviewComment> comments)
    {
        var body = new
        {
            @event = "COMMENT",
            comments = comments.Select(c => new
            {
                path = c.Path,
                line = c.Line,
                body = c.Comment,
                side = "RIGHT"
            })
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"/repos/{owner}/{repo}/pulls/{prNumber}/reviews", content);
        response.EnsureSuccessStatusCode();
    }

    private sealed class GitHubFile
    {
        [JsonPropertyName("filename")] public string Filename { get; set; } = string.Empty;
        [JsonPropertyName("patch")] public string? Patch { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
        [JsonPropertyName("additions")] public int Additions { get; set; }
        [JsonPropertyName("deletions")] public int Deletions { get; set; }
    }
}
