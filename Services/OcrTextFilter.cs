using BookSharingApp.Common;
using BookSharingWebAPI.Models;

namespace BookSharingWebAPI.Services;

public static class OcrTextFilter
{
    public static List<ExtractedWord> ExtractFilteredText(List<OcrLine> ocrLines, ILogger? logger = null)
    {
        // Calculate maximum text size to identify what's "large"
        // Using GetTextSize() instead of GetHeight() to handle vertical text correctly
        var linesWithSize = ocrLines.Where(l => l.GetTextSize() > 0).ToList();

        if (linesWithSize.Count == 0)
        {
            // Fallback to basic filtering if no bounding box data - extract words from lines
            return ocrLines
                .SelectMany(line => line.Text
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(word => word.Length >= ImageAnalysisConstants.MinTitleLength &&
                                  word.Length <= ImageAnalysisConstants.MaxTitleLength)
                    .Select(word => new ExtractedWord { Text = word, Height = 0 }))
                .ToList();
        }

        // Get the size of the largest text
        var maxSize = linesWithSize.Max(l => l.GetTextSize());

        // Keep lines that are at least x% of the largest text size
        // This captures title and author while filtering out small promotional text and vertical spine text
        var sizeThreshold = maxSize * ImageAnalysisConstants.TextSizeFilterThresholdPercentage;

        // Extract words from large text lines, preserving order (top to bottom, left to right)
        var extractedWords = ocrLines
            .Where(l => l.GetTextSize() >= sizeThreshold)
            .SelectMany(line => line.Text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => new ExtractedWord
                {
                    Text = word,
                    Height = line.GetTextSize() // Store size (not just height) for retry logic
                }))
            .ToList();

        logger?.LogDebug(
            "Text filtering: MaxSize={MaxSize}, Threshold={Threshold}, " +
            "TotalLines={Total}, ExtractedWords={WordCount}",
            maxSize, sizeThreshold, ocrLines.Count, extractedWords.Count);

        // Apply word count limits
        return ApplyWordCountLimits(extractedWords, logger);
    }

    private static List<ExtractedWord> ApplyWordCountLimits(List<ExtractedWord> words, ILogger? logger = null)
    {
        var wordCount = words.Count;

        // Within range - return as-is
        if (wordCount >= ImageAnalysisConstants.MinSearchWords &&
            wordCount <= ImageAnalysisConstants.MaxSearchWords)
        {
            return words;
        }

        // Too few words - keep all we have
        if (wordCount < ImageAnalysisConstants.MinSearchWords)
        {
            logger?.LogDebug("Word count {Count} below minimum {Min}, keeping all words",
                wordCount, ImageAnalysisConstants.MinSearchWords);
            return words;
        }

        // Too many words - keep largest words up to max limit
        logger?.LogDebug("Word count {Count} exceeds maximum {Max}, keeping largest words",
            wordCount, ImageAnalysisConstants.MaxSearchWords);

        // Sort by height descending (largest first), take max allowed
        var largestWords = words
            .OrderByDescending(w => w.Height)
            .Take(ImageAnalysisConstants.MaxSearchWords)
            .ToList();

        // Restore original order (top to bottom, left to right)
        var originalOrder = words
            .Where(w => largestWords.Contains(w))
            .ToList();

        logger?.LogDebug("Trimmed from {Original} to {Final} words",
            wordCount, originalOrder.Count);

        return originalOrder;
    }
}
