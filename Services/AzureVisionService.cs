using System.Net.Http.Headers;
using System.Text.Json;
using BookSharingWebAPI.Models;
using BookSharingApp.Common;

namespace BookSharingWebAPI.Services;

public class AzureVisionService : IImageAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureVisionService> _logger;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly int _pollingDelayMs;

    public AzureVisionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AzureVisionService> logger,
        int pollingDelayMs = ImageAnalysisConstants.OcrPollingDelayMs)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pollingDelayMs = pollingDelayMs;
        _endpoint = configuration["AzureVision:Endpoint"]
            ?? throw new InvalidOperationException("AzureVision:Endpoint not configured");
        _apiKey = configuration["AzureVision:ApiKey"]
            ?? throw new InvalidOperationException("AzureVision:ApiKey not configured");
    }

    public async Task<CoverAnalysisResult> AnalyzeCoverImageAsync(
        Stream imageStream, string contentType, CancellationToken cancellationToken = default)
    {
        // 1. Read image bytes
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();

        // 2. Call Azure Vision OCR
        List<LineResult> ocrLines = await CallAzureOcrAsync(imageBytes, contentType, cancellationToken);

        // 3. Filter based on text size and extract words
        var filteredWords = ExtractFilteredText(ocrLines);

        _logger.LogInformation(
            "Cover analysis complete. Filtered words: {FilteredCount}/{TotalCount}",
            filteredWords.Count, ocrLines.Count);

        var rawText = ocrLines.Select(l => l.Text).ToList();
        return CoverAnalysisResult.Success(filteredWords, rawText);
    }

    private async Task<List<LineResult>> CallAzureOcrAsync(
        byte[] imageBytes, string contentType, CancellationToken cancellationToken)
    {
        var requestUrl = $"{_endpoint}/vision/v3.2/read/analyze";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        request.Content = new ByteArrayContent(imageBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Azure returns operation location header for async processing
        if (!response.Headers.TryGetValues("Operation-Location", out var locationValues) ||
            locationValues == null || !locationValues.Any())
        {
            throw new InvalidOperationException(
                "Azure Vision API did not return Operation-Location header. " +
                "This may indicate an API error or misconfiguration.");
        }

        var operationLocation = locationValues.First();

        // Poll for results
        return await PollForOcrResultsAsync(operationLocation, cancellationToken);
    }

    private async Task<List<LineResult>> PollForOcrResultsAsync(
        string operationUrl, CancellationToken cancellationToken)
    {
        for (int i = 0; i < ImageAnalysisConstants.MaxOcrPollingAttempts; i++)
        {
            await Task.Delay(_pollingDelayMs, cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Get, operationUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<AzureReadResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result?.Status == "succeeded")
            {
                return result.AnalyzeResult?.ReadResults?
                    .SelectMany(r => r.Lines)
                    .ToList() ?? new List<LineResult>();
            }

            if (result?.Status == "failed")
                throw new InvalidOperationException("Azure OCR processing failed");
        }

        throw new TimeoutException("OCR processing timed out");
    }

    private List<ExtractedWord> ExtractFilteredText(List<LineResult> ocrLines)
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

        _logger.LogDebug(
            "Text filtering: MaxSize={MaxSize}, Threshold={Threshold}, " +
            "TotalLines={Total}, ExtractedWords={WordCount}",
            maxSize, sizeThreshold, ocrLines.Count, extractedWords.Count);

        // Apply word count limits
        return ApplyWordCountLimits(extractedWords);
    }

    private List<ExtractedWord> ApplyWordCountLimits(List<ExtractedWord> words)
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
            _logger.LogDebug("Word count {Count} below minimum {Min}, keeping all words",
                wordCount, ImageAnalysisConstants.MinSearchWords);
            return words;
        }

        // Too many words - keep largest words up to max limit
        _logger.LogDebug("Word count {Count} exceeds maximum {Max}, keeping largest words",
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

        _logger.LogDebug("Trimmed from {Original} to {Final} words",
            wordCount, originalOrder.Count);

        return originalOrder;
    }
}

// Helper classes for JSON deserialization
internal class AzureReadResult
{
    public string Status { get; set; } = "";
    public AnalyzeResultData? AnalyzeResult { get; set; }
}

internal class AnalyzeResultData
{
    public List<ReadResult>? ReadResults { get; set; }
}

internal class ReadResult
{
    public List<LineResult> Lines { get; set; } = new();
}

internal class LineResult
{
    public string Text { get; set; } = "";
    public List<double>? BoundingBox { get; set; }

    public double GetHeight()
    {
        if (BoundingBox == null || BoundingBox.Count < 8)
            return 0;

        // BoundingBox is [x1,y1, x2,y2, x3,y3, x4,y4]
        // Height is roughly the difference between top and bottom y coordinates
        var topY = Math.Min(BoundingBox[1], BoundingBox[3]);
        var bottomY = Math.Max(BoundingBox[5], BoundingBox[7]);
        return bottomY - topY;
    }

    public double GetWidth()
    {
        if (BoundingBox == null || BoundingBox.Count < 8)
            return 0;

        // Width is the difference between left and right x coordinates
        var leftX = Math.Min(BoundingBox[0], BoundingBox[6]);
        var rightX = Math.Max(BoundingBox[2], BoundingBox[4]);
        return rightX - leftX;
    }

    /// <summary>
    /// Determines if text is oriented vertically based on bounding box geometry.
    /// BoundingBox points: [x1,y1, x2,y2, x3,y3, x4,y4] (typically top-left, top-right, bottom-right, bottom-left)
    /// </summary>
    public bool IsVertical()
    {
        if (BoundingBox == null || BoundingBox.Count < 8)
            return false;

        // Calculate the vector from first point to second point (first edge)
        var dx = Math.Abs(BoundingBox[2] - BoundingBox[0]);
        var dy = Math.Abs(BoundingBox[3] - BoundingBox[1]);

        // If the first edge is more vertical than horizontal, text is vertical
        return dy > dx;
    }

    /// <summary>
    /// Gets the text size (font size), correctly handling both horizontal and vertical orientations.
    /// - For horizontal text: uses height (vertical extent of characters)
    /// - For vertical text: uses width (horizontal extent of characters)
    /// </summary>
    public double GetTextSize()
    {
        var height = GetHeight();
        var width = GetWidth();

        if (height == 0 || width == 0)
            return 0;

        // For vertical text, width represents the font size
        // For horizontal text, height represents the font size
        return IsVertical() ? width : height;
    }
}

