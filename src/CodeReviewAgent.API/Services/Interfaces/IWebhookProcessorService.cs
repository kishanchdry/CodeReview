using CodeReviewAgent.API.Models.Webhook;

namespace CodeReviewAgent.API.Services.Interfaces;

public interface IWebhookProcessorService
{
    Task ProcessAsync(GitHubWebhookPayload payload);
}
