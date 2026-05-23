namespace CodeReviewAgent.API.Configuration;

public class GitSettings
{
    public string Provider { get; set; } = "GitHub";
    public string BaseUrl { get; set; } = "https://api.github.com";
    public string Token { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string RepoOwner { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
}
