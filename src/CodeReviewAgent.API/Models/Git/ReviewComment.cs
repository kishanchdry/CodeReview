using System.Text.Json.Serialization;

namespace CodeReviewAgent.API.Models.Git;

public class ReviewComment
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}
