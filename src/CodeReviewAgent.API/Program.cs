using CodeReviewAgent.API.Configuration;
using CodeReviewAgent.API.Middleware;
using CodeReviewAgent.API.Services;
using CodeReviewAgent.API.Services.Interfaces;
using CodeReviewAgent.API.Services.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GitSettings>(builder.Configuration.GetSection("Git"));
builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<WebhookSettings>(builder.Configuration.GetSection("Webhook"));

builder.Services.AddHttpClient<IGitService, GitHubService>();
builder.Services.AddHttpClient();

builder.Services.AddScoped<IAIProvider>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    return config["AI:Provider"] == "Claude"
        ? new ClaudeProvider(http, config)
        : new OpenAIProvider(http, config);
});

builder.Services.AddScoped<IAIReviewService, AIReviewService>();
builder.Services.AddScoped<IWebhookProcessorService, WebhookProcessorService>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseMiddleware<WebhookSignatureMiddleware>();
app.MapControllers();

app.Run();
