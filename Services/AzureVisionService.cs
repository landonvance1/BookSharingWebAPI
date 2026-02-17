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
        List<OcrLine> ocrLines = await CallAzureOcrAsync(imageBytes, contentType, cancellationToken);

        // 3. Filter based on text size and extract words
        var filteredWords = OcrTextFilter.ExtractFilteredText(ocrLines, _logger);

        _logger.LogInformation(
            "Cover analysis complete. Filtered words: {FilteredCount}/{TotalCount}",
            filteredWords.Count, ocrLines.Count);

        var rawText = ocrLines.Select(l => l.Text).ToList();
        return CoverAnalysisResult.Success(filteredWords, rawText);
    }

    private async Task<List<OcrLine>> CallAzureOcrAsync(
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

    private async Task<List<OcrLine>> PollForOcrResultsAsync(
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
                    .ToList() ?? new List<OcrLine>();
            }

            if (result?.Status == "failed")
                throw new InvalidOperationException("Azure OCR processing failed");
        }

        throw new TimeoutException("OCR processing timed out");
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
    public List<OcrLine> Lines { get; set; } = new();
}
