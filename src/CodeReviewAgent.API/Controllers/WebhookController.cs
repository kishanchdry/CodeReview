using System.Text;
using System.Text.Json;
using CodeReviewAgent.API.Models.Webhook;
using CodeReviewAgent.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CodeReviewAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IServiceScopeFactory scopeFactory, ILogger<WebhookController> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost("github")]
    public async Task<IActionResult> GitHub()
    {
        Request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        GitHubWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GitHubWebhookPayload>(body);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid webhook payload");
            return BadRequest();
        }

        if (payload is null) return BadRequest();

        if (payload.Action is not ("opened" or "synchronize"))
            return Ok();

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IWebhookProcessorService>();
            await processor.ProcessAsync(payload);
        });

        return Ok();
    }
}
