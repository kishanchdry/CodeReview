namespace CodeReviewAgent.API.Services.Providers;

public interface IAIProvider
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt);
}
