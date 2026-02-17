namespace BookSharingWebAPI.Models;

/// <summary>
/// Simplified summary of cover image analysis results for client consumption.
/// </summary>
public class CoverAnalysisSummary
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
}
