using CodeReviewAgent.API.Models.Git;

namespace CodeReviewAgent.API.Services.Interfaces;

public interface IAIReviewService
{
    Task<List<ReviewComment>> ReviewCodeAsync(List<FileDiff> diffs);
}
