using CodeReviewAgent.API.Models.Git;

namespace CodeReviewAgent.API.Services.Interfaces;

public interface IGitService
{
    Task<List<FileDiff>> GetPullRequestDiffAsync(string owner, string repo, int prNumber);
    Task PostReviewCommentsAsync(string owner, string repo, int prNumber, List<ReviewComment> comments);
}
