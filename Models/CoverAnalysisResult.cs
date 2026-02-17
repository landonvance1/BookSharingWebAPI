namespace BookSharingWebAPI.Models;

public class CoverAnalysisResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ExtractedWord> FilteredText { get; set; } = new();
    public List<string> RawExtractedText { get; set; } = new();

    public static CoverAnalysisResult Success(
        List<ExtractedWord> filteredWords,
        List<string> rawText)
    {
        return new CoverAnalysisResult
        {
            IsSuccess = true,
            FilteredText = filteredWords,
            RawExtractedText = rawText
        };
    }

    public static CoverAnalysisResult Failure(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message
    };
}
