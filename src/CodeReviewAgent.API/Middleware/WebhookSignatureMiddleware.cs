using System.Security.Cryptography;
using System.Text;
using CodeReviewAgent.API.Configuration;
using Microsoft.Extensions.Options;

namespace CodeReviewAgent.API.Middleware;

public class WebhookSignatureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WebhookSettings _settings;

    public WebhookSignatureMiddleware(RequestDelegate next, IOptions<WebhookSettings> settings)
    {
        _next = next;
        _settings = settings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/webhook"))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        if (_settings.ValidateSignature && !string.IsNullOrEmpty(_settings.Secret))
        {
            var signature = context.Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            if (!ValidateSignature(body, signature))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await _next(context);
    }

    private bool ValidateSignature(string body, string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;

        var key = Encoding.UTF8.GetBytes(_settings.Secret);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(body));
        var expected = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }
}
