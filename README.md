# CodeReview
This is repo to review code by AI

# 🤖 Code Review Agent — Build Prompt

## 🎯 Agent Objective

You are a senior .NET Core architect. Build a **production-ready AI-powered Code Review Agent** as a `.NET Core Web API` application that:

1. Receives **Git webhook events** (Pull Request opened/updated)
2. Fetches the **PR diff/changed files** from Git (GitHub/GitLab)
3. Sends the **diff to an AI** (OpenAI GPT or Claude) for code review
4. Posts **AI-generated review comments** back to the Git PR via Git API
5. Is **containerized with Docker** and testable locally via **ngrok**

---

## 📁 Project Structure

Generate the following folder and file structure exactly:

```
CodeReviewAgent/
├── CodeReviewAgent.sln
├── docker-compose.yml
├── ngrok.yml                          # ngrok tunnel config
│
└── src/
    └── CodeReviewAgent.API/
        ├── CodeReviewAgent.API.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── Dockerfile
        │
        ├── Controllers/
        │   └── WebhookController.cs   # Receives Git webhook POST
        │
        ├── Services/
        │   ├── Interfaces/
        │   │   ├── IGitService.cs
        │   │   ├── IAIReviewService.cs
        │   │   └── IWebhookProcessorService.cs
        │   ├── GitHubService.cs       # Git API: fetch diff, post comments
        │   ├── AIReviewService.cs     # Calls OpenAI/Claude API
        │   └── WebhookProcessorService.cs  # Orchestrates the full flow
        │
        ├── Models/
        │   ├── Webhook/
        │   │   ├── GitHubWebhookPayload.cs
        │   │   └── PullRequestEvent.cs
        │   ├── Git/
        │   │   ├── PullRequestDiff.cs
        │   │   ├── FileDiff.cs
        │   │   └── ReviewComment.cs
        │   └── AI/
        │       ├── AIReviewRequest.cs
        │       └── AIReviewResponse.cs
        │
        ├── Configuration/
        │   ├── GitSettings.cs         # Git token, repo, owner config
        │   ├── AISettings.cs          # AI provider, model, token limits
        │   └── WebhookSettings.cs     # Webhook secret for validation
        │
        ├── Prompts/
        │   ├── SystemPrompts/
        │   │   └── CodeReviewSystem.txt     # AI system role definition
        │   ├── UserPrompts/
        │   │   └── CodeReviewUser.txt       # User prompt template with {diff} placeholder
        │   └── Guidelines/
        │       ├── GeneralGuidelines.txt    # General code review rules
        │       ├── SecurityGuidelines.txt   # Security-specific review rules
        │       └── PerformanceGuidelines.txt # Performance review rules
        │
        └── Middleware/
            └── WebhookSignatureMiddleware.cs  # Validates GitHub HMAC signature
```

---

## ⚙️ appsettings.json — Configuration Schema

```json
{
  "Git": {
    "Provider": "GitHub",
    "BaseUrl": "https://api.github.com",
    "Token": "",
    "WebhookSecret": "",
    "RepoOwner": "",
    "RepoName": ""
  },
  "AI": {
    "Provider": "OpenAI",
    "ApiKey": "",
    "Model": "gpt-4o-mini",
    "MaxInputTokens": 3000,
    "MaxOutputTokens": 800,
    "Temperature": 0.3,
    "AlternativeProvider": "Claude",
    "ClaudeApiKey": "",
    "ClaudeModel": "claude-haiku-4-5-20251001"
  },
  "Webhook": {
    "Secret": "",
    "ValidateSignature": true
  }
}
```

---

## 🧩 Core Implementation Requirements

### 1. `WebhookController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    // POST api/webhook/github
    // - Read raw body for signature validation
    // - Validate X-Hub-Signature-256 header
    // - Deserialize payload
    // - Trigger IWebhookProcessorService only for "opened" and "synchronize" PR actions
    // - Return 200 OK immediately (fire-and-forget using Task.Run or BackgroundService)
}
```

### 2. `WebhookProcessorService.cs`

```csharp
// Orchestration flow:
// 1. Extract PR number, repo, owner from payload
// 2. Call IGitService.GetPullRequestDiffAsync() → list of FileDiff
// 3. Filter: skip binary files, skip files > MaxInputTokens threshold
// 4. Call IAIReviewService.ReviewCodeAsync(filteredDiffs) → list of ReviewComment
// 5. Call IGitService.PostReviewCommentsAsync(comments)
```

### 3. `GitHubService.cs`

```csharp
// Implement IGitService:
// GetPullRequestDiffAsync(owner, repo, prNumber)
//   → GET /repos/{owner}/{repo}/pulls/{prNumber}/files
//   → Parse patch field for each file
//   → Return List<FileDiff> { FileName, Patch, Status, Additions, Deletions }

// PostReviewCommentsAsync(owner, repo, prNumber, comments)
//   → POST /repos/{owner}/{repo}/pulls/{prNumber}/reviews
//   → Use "COMMENT" event type
//   → Batch all comments into a single review API call (minimize API calls)
```

### 4. `AIReviewService.cs`

```csharp
// Implement IAIReviewService:
// ReviewCodeAsync(List<FileDiff> diffs)
//   1. Load system prompt from Prompts/SystemPrompts/CodeReviewSystem.txt
//   2. Load guidelines from Prompts/Guidelines/*.txt
//   3. Load user prompt template from Prompts/UserPrompts/CodeReviewUser.txt
//   4. Build final prompt: inject diff into template
//   5. Truncate diff if it exceeds MaxInputTokens (use tiktoken-style char estimate)
//   6. Call AI API (OpenAI or Claude based on config)
//   7. Parse response → extract file, line, comment structured output
//   8. Return List<ReviewComment>
```

---

## 📝 Prompts — File Contents to Generate

### `Prompts/SystemPrompts/CodeReviewSystem.txt`

```
You are an expert software engineer conducting a pull request code review.
Your role is to identify bugs, security vulnerabilities, performance issues,
and violations of clean code principles.

Rules:
- Be concise and actionable. No praise unless critical for context.
- Focus only on the changed lines in the diff.
- Output ONLY a valid JSON array of review comments.
- Do not add any explanation outside the JSON.

Output format:
[
  {
    "path": "src/MyService.cs",
    "line": 42,
    "comment": "Potential null reference. Add null check before accessing .Value"
  }
]
```

### `Prompts/UserPrompts/CodeReviewUser.txt`

```
Review the following pull request diff and identify issues.
Apply the coding guidelines provided.

Guidelines:
{guidelines}

PR Diff:
{diff}

Return ONLY a JSON array of comments as specified. Be brief.
```

### `Prompts/Guidelines/GeneralGuidelines.txt`

```
- Use meaningful variable and method names
- Avoid magic numbers and strings; use constants or enums
- Methods should do one thing (Single Responsibility)
- Remove dead/commented code
- Ensure proper null checks and error handling
- Avoid deep nesting; prefer early returns
- Log errors with sufficient context
```

### `Prompts/Guidelines/SecurityGuidelines.txt`

```
- Never log sensitive data (passwords, tokens, PII)
- Validate and sanitize all user inputs
- Avoid SQL string concatenation; use parameterized queries
- Do not expose stack traces in API responses
- Ensure proper authentication/authorization on all endpoints
- Avoid hardcoded secrets or credentials
```

### `Prompts/Guidelines/PerformanceGuidelines.txt`

```
- Avoid N+1 database queries; use batch fetching
- Use async/await correctly; avoid .Result or .Wait()
- Avoid unnecessary object allocations in loops
- Cache expensive operations where appropriate
- Use pagination for large data sets
```

---

## 🐳 Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/CodeReviewAgent.API/CodeReviewAgent.API.csproj", "CodeReviewAgent.API/"]
RUN dotnet restore "CodeReviewAgent.API/CodeReviewAgent.API.csproj"
COPY src/CodeReviewAgent.API/ CodeReviewAgent.API/
WORKDIR "/src/CodeReviewAgent.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeReviewAgent.API.dll"]
```

## 🐳 docker-compose.yml

```yaml
version: '3.8'
services:
  codereviewagent:
    build:
      context: .
      dockerfile: src/CodeReviewAgent.API/Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Git__Token=${GIT_TOKEN}
      - Git__WebhookSecret=${WEBHOOK_SECRET}
      - AI__ApiKey=${OPENAI_API_KEY}
    volumes:
      - ./src/CodeReviewAgent.API/Prompts:/app/Prompts:ro
```

---

## 🌐 ngrok Configuration (`ngrok.yml`)

```yaml
version: "2"
tunnels:
  codereview:
    proto: http
    addr: 8080
    inspect: true
```

**Local Testing Steps:**
1. Run: `docker-compose up`
2. Run: `ngrok start --config ngrok.yml codereview`
3. Copy the ngrok HTTPS URL
4. Set as GitHub Webhook URL: `https://<ngrok-url>/api/webhook/github`
5. Content type: `application/json`
6. Events: `Pull requests` only

---

## 🔒 Token Minimization Strategy

Implement these rules to reduce AI token usage:

| Strategy | Implementation |
|---|---|
| **File filtering** | Skip `*.json`, `*.md`, `*.yml`, `*.lock`, `*.csproj` files |
| **Diff truncation** | Truncate diff to `MaxInputTokens` chars before sending |
| **Single AI call** | Batch all file diffs into ONE AI call per PR (not per file) |
| **Haiku/mini model** | Default to `gpt-4o-mini` or `claude-haiku` for cost |
| **Cached guidelines** | Load guidelines once at startup, inject as static string |
| **Max line limit** | Only send files with < 500 changed lines |
| **Additions only** | Send only added lines (`+` prefix in diff) for large files |

---

## 🔑 Key NuGet Packages

```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="8.*" />
<PackageReference Include="System.Text.Json" Version="8.*" />
<PackageReference Include="Octokit" Version="*" />           <!-- GitHub API client -->
<PackageReference Include="OpenAI" Version="2.*" />          <!-- OpenAI official SDK -->
<PackageReference Include="Anthropic.SDK" Version="*" />     <!-- Claude SDK (optional) -->
```

---

## 🔄 AI Provider Abstraction

Use a factory pattern so providers are swappable via config:

```csharp
public interface IAIProvider
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt);
}

public class OpenAIProvider : IAIProvider { ... }
public class ClaudeProvider : IAIProvider { ... }

// In Program.cs:
builder.Services.AddScoped<IAIProvider>(sp =>
    config["AI:Provider"] == "Claude"
        ? new ClaudeProvider(config)
        : new OpenAIProvider(config));
```

---

## ✅ Deliverables Checklist

- [ ] Full `.NET 8` Web API project with all files above
- [ ] `WebhookController` with HMAC-SHA256 signature validation
- [ ] `GitHubService` using GitHub REST API (`/pulls/{id}/files` + `/pulls/{id}/reviews`)
- [ ] `AIReviewService` with OpenAI as default, Claude as fallback
- [ ] All 5 prompt/guideline `.txt` files loaded from disk at runtime
- [ ] Token minimization logic (file filter + truncation + batching)
- [ ] `Dockerfile` and `docker-compose.yml`
- [ ] `ngrok.yml` and local testing instructions in `README.md`
- [ ] `appsettings.json` with all config keys (no hardcoded secrets)
- [ ] Dependency Injection wired in `Program.cs`
