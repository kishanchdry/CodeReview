using CodeReviewAgent.API.Models.Webhook;
using CodeReviewAgent.API.Services.Interfaces;

namespace CodeReviewAgent.API.Services;

public class WebhookProcessorService : IWebhookProcessorService
{
    private readonly IGitService _git;
    private readonly IAIReviewService _ai;
    private readonly ILogger<WebhookProcessorService> _logger;

    public WebhookProcessorService(IGitService git, IAIReviewService ai, ILogger<WebhookProcessorService> logger)
    {
        _git = git;
        _ai = ai;
        _logger = logger;
    }

    public async Task ProcessAsync(GitHubWebhookPayload payload)
    {
        var owner = payload.Repository.Owner.Login;
        var repo = payload.Repository.Name;
        var prNumber = payload.Number;

        try
        {
            _logger.LogInformation("Processing PR #{PrNumber} in {Owner}/{Repo}", prNumber, owner, repo);

            var diffs = await _git.GetPullRequestDiffAsync(owner, repo, prNumber);
            var comments = await _ai.ReviewCodeAsync(diffs);

            if (comments.Count > 0)
                await _git.PostReviewCommentsAsync(owner, repo, prNumber, comments);

            _logger.LogInformation("Posted {Count} review comments on PR #{PrNumber}", comments.Count, prNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PR #{PrNumber}", prNumber);
        }
    }
}
