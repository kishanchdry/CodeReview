namespace CodeReviewAgent.API.Configuration;

public class WebhookSettings
{
    public string Secret { get; set; } = string.Empty;
    public bool ValidateSignature { get; set; } = true;
}
