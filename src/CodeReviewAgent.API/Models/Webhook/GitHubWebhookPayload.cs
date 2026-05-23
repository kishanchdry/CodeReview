using System.Text.Json.Serialization;

namespace CodeReviewAgent.API.Models.Webhook;

public class GitHubWebhookPayload
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("pull_request")]
    public PullRequestInfo PullRequest { get; set; } = new();

    [JsonPropertyName("repository")]
    public RepositoryInfo Repository { get; set; } = new();
}

public class PullRequestInfo
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("head")]
    public CommitRef Head { get; set; } = new();
}

public class CommitRef
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
}

public class RepositoryInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public OwnerInfo Owner { get; set; } = new();
}

public class OwnerInfo
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
}
