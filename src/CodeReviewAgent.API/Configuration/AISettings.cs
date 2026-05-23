namespace CodeReviewAgent.API.Configuration;

public class AISettings
{
    public string Provider { get; set; } = "OpenAI";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxInputTokens { get; set; } = 3000;
    public int MaxOutputTokens { get; set; } = 800;
    public double Temperature { get; set; } = 0.3;
    public string AlternativeProvider { get; set; } = "Claude";
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string ClaudeModel { get; set; } = "claude-haiku-4-5-20251001";
}
