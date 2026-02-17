namespace BookSharingWebAPI.Models;

/// <summary>
/// Represents a single word extracted from OCR with its visual size information.
/// Used for intelligent retry logic that can remove smaller/less important words.
/// </summary>
public class ExtractedWord
{
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Visual size of the text (minimum of width/height from bounding box).
    /// This correctly represents text size regardless of orientation (horizontal or vertical).
    /// Larger values indicate more prominent text (likely title/author).
    /// </summary>
    public double Height { get; set; }
}
