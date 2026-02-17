using System.Diagnostics;
using BookSharingWebAPI.Models;

namespace BookSharingWebAPI.Services;

public class TesseractService : IImageAnalysisService
{
    private readonly ILogger<TesseractService> _logger;
    private readonly string _language;
    private readonly string? _tessDataPath; // optional: sets TESSDATA_PREFIX for non-standard installs

    public TesseractService(IConfiguration configuration, ILogger<TesseractService> logger)
    {
        _logger = logger;
        _language = configuration["Tesseract:Language"] ?? "eng";
        _tessDataPath = configuration["Tesseract:TessDataPath"];
    }

    public async Task<CoverAnalysisResult> AnalyzeCoverImageAsync(
        Stream imageStream, string contentType, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();

        var ocrLines = await RunTesseractCliAsync(imageBytes, contentType, cancellationToken);
        var filteredWords = OcrTextFilter.ExtractFilteredText(ocrLines, _logger);

        _logger.LogInformation(
            "Cover analysis complete. Filtered words: {FilteredCount}/{TotalCount}",
            filteredWords.Count, ocrLines.Count);

        var rawText = ocrLines.Select(l => l.Text).ToList();
        return CoverAnalysisResult.Success(filteredWords, rawText);
    }

    private async Task<List<OcrLine>> RunTesseractCliAsync(
        byte[] imageBytes, string contentType, CancellationToken cancellationToken)
    {
        var ext = contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };

        var tempImagePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ext);
        try
        {
            await File.WriteAllBytesAsync(tempImagePath, imageBytes, cancellationToken);

            var psi = new ProcessStartInfo
            {
                FileName = "tesseract",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            psi.ArgumentList.Add(tempImagePath);
            psi.ArgumentList.Add("stdout");
            psi.ArgumentList.Add("-l");
            psi.ArgumentList.Add(_language);
            psi.ArgumentList.Add("tsv");

            if (!string.IsNullOrEmpty(_tessDataPath))
                psi.Environment["TESSDATA_PREFIX"] = _tessDataPath;

            using var process = new Process { StartInfo = psi };
            process.Start();

            // Read stdout and stderr concurrently to avoid deadlocking on full buffers
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await stdoutTask;
            if (process.ExitCode != 0)
            {
                var stderr = await stderrTask;
                throw new InvalidOperationException(
                    $"Tesseract CLI failed (exit {process.ExitCode}): {stderr}");
            }

            return ParseTsvOutput(output);
        }
        finally
        {
            if (File.Exists(tempImagePath))
                File.Delete(tempImagePath);
        }
    }

    /// <summary>
    /// Parses Tesseract TSV output into line-level OcrLine objects.
    ///
    /// TSV columns: level page_num block_num par_num line_num word_num left top width height conf text
    /// Level 5 rows are individual words. We group words by (page, block, par, line) key,
    /// joining text and computing the union bounding box across all words in the line.
    /// </summary>
    public static List<OcrLine> ParseTsvOutput(string tsv)
    {
        // Use a SortedDictionary so lines come out in reading order (page → block → par → line)
        var wordsByLine = new SortedDictionary<(int page, int block, int par, int line),
            (int left, int top, int right, int bottom, List<string> words)>();

        foreach (var row in tsv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var parts = row.Split('\t');
            if (parts.Length < 12) continue;
            if (!int.TryParse(parts[0], out int level) || level != 5) continue;
            if (!int.TryParse(parts[1], out int page)) continue;
            if (!int.TryParse(parts[2], out int block)) continue;
            if (!int.TryParse(parts[3], out int par)) continue;
            if (!int.TryParse(parts[4], out int lineNum)) continue;
            if (!int.TryParse(parts[6], out int left)) continue;
            if (!int.TryParse(parts[7], out int top)) continue;
            if (!int.TryParse(parts[8], out int width) || width <= 0) continue;
            if (!int.TryParse(parts[9], out int height) || height <= 0) continue;
            if (!double.TryParse(parts[10], System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out double conf) || conf < 0) continue; // conf=-1 = non-word marker

            var wordText = parts[11].Trim();
            if (string.IsNullOrWhiteSpace(wordText)) continue;

            var key = (page, block, par, lineNum);
            int right = left + width;
            int bottom = top + height;

            if (wordsByLine.TryGetValue(key, out var existing))
            {
                // Expand the line's bounding box to include this word
                wordsByLine[key] = (
                    Math.Min(existing.left, left),
                    Math.Min(existing.top, top),
                    Math.Max(existing.right, right),
                    Math.Max(existing.bottom, bottom),
                    existing.words
                );
                existing.words.Add(wordText);
            }
            else
            {
                wordsByLine[key] = (left, top, right, bottom, new List<string> { wordText });
            }
        }

        var result = new List<OcrLine>();
        foreach (var (_, (l, t, r, b, words)) in wordsByLine)
        {
            result.Add(new OcrLine
            {
                Text = string.Join(" ", words),
                // 8-point polygon: top-left, top-right, bottom-right, bottom-left
                BoundingBox = new List<double> { l, t, r, t, r, b, l, b }
            });
        }
        return result;
    }
}
