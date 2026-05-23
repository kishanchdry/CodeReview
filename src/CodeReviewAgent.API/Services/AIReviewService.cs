using System.Text;
using System.Text.Json;
using CodeReviewAgent.API.Configuration;
using CodeReviewAgent.API.Models.Git;
using CodeReviewAgent.API.Services.Interfaces;
using CodeReviewAgent.API.Services.Providers;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.API.Services;

public class AIReviewService : IAIReviewService
{
    private readonly IAIProvider _provider;
    private readonly AISettings _settings;
    private readonly string _systemPrompt;
    private readonly string _userPromptTemplate;
    private readonly string _guidelines;

    private static readonly HashSet<string> SkippedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".json", ".md", ".yml", ".yaml", ".lock", ".csproj", ".sln", ".txt", ".xml" };

    public AIReviewService(IAIProvider provider, IOptions<AISettings> settings, IWebHostEnvironment env)
    {
        _provider = provider;
        _settings = settings.Value;

        var promptsBase = Path.Combine(env.ContentRootPath, "Prompts");
        _systemPrompt = File.ReadAllText(Path.Combine(promptsBase, "SystemPrompts", "CodeReviewSystem.txt"));
        _userPromptTemplate = File.ReadAllText(Path.Combine(promptsBase, "UserPrompts", "CodeReviewUser.txt"));
        _guidelines = string.Join("\n\n", Directory.GetFiles(Path.Combine(promptsBase, "Guidelines"), "*.txt")
            .Select(File.ReadAllText));
    }

    public async Task<List<ReviewComment>> ReviewCodeAsync(List<FileDiff> diffs)
    {
        var filtered = diffs
            .Where(f => !SkippedExtensions.Contains(Path.GetExtension(f.FileName)))
            .Where(f => f.Additions + f.Deletions <= 500)
            .Where(f => !string.IsNullOrWhiteSpace(f.Patch))
            .ToList();

        if (filtered.Count == 0) return [];

        var diffText = BuildDiffText(filtered);
        var userPrompt = _userPromptTemplate
            .Replace("{guidelines}", _guidelines)
            .Replace("{diff}", diffText);

        var response = await _provider.CompleteAsync(_systemPrompt, userPrompt);
        return ParseResponse(response);
    }

    private string BuildDiffText(List<FileDiff> diffs)
    {
        var sb = new StringBuilder();
        var charLimit = _settings.MaxInputTokens * 4;

        foreach (var diff in diffs)
        {
            var entry = $"### {diff.FileName}\n{diff.Patch}\n\n";
            if (sb.Length + entry.Length > charLimit) break;
            sb.Append(entry);
        }

        return sb.ToString();
    }

    private static List<ReviewComment> ParseResponse(string response)
    {
        try
        {
            var start = response.IndexOf('[');
            var end = response.LastIndexOf(']');
            if (start < 0 || end <= start) return [];

            var json = response[start..(end + 1)];
            return JsonSerializer.Deserialize<List<ReviewComment>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
