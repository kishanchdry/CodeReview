namespace CodeReviewAgent.API.Models.Git;

public class FileDiff
{
    public string FileName { get; set; } = string.Empty;
    public string Patch { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Additions { get; set; }
    public int Deletions { get; set; }
}
