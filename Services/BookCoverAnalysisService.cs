using Microsoft.EntityFrameworkCore;
using BookSharingApp.Data;
using BookSharingApp.Models;
using BookSharingApp.Services;
using BookSharingApp.Common;
using BookSharingWebAPI.Models;

namespace BookSharingWebAPI.Services;

public class BookCoverAnalysisService : IBookCoverAnalysisService
{
    private readonly IImageAnalysisService _imageAnalysisService;
    private readonly IBookLookupService _bookLookupService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BookCoverAnalysisService> _logger;

    public BookCoverAnalysisService(
        IImageAnalysisService imageAnalysisService,
        IBookLookupService bookLookupService,
        ApplicationDbContext context,
        ILogger<BookCoverAnalysisService> logger)
    {
        _imageAnalysisService = imageAnalysisService;
        _bookLookupService = bookLookupService;
        _context = context;
        _logger = logger;
    }

    public async Task<CoverAnalysisResponse> AnalyzeCoverAsync(
        Stream imageStream, string contentType, string requestId, CancellationToken cancellationToken = default)
    {
        var ocrResult = await _imageAnalysisService.AnalyzeCoverImageAsync(imageStream, contentType, cancellationToken);

        if (!ocrResult.IsSuccess)
        {
            return FailureResponse(ocrResult.ErrorMessage);
        }

        var scoredBooks = await SearchForMatchingBooksAsync(ocrResult, requestId);

        _logger.LogInformation(
            "Cover analysis completed [RequestId={RequestId}, OcrLinesExtracted={OcrLines}, " +
            "TotalMatches={TotalMatches}, LocalMatches={LocalMatches}, ExternalMatches={ExternalMatches}]",
            requestId, ocrResult.RawExtractedText.Count,
            scoredBooks.Count, scoredBooks.Count(m => m.book.Id > 0), scoredBooks.Count(m => m.book.Id < 0));

        var exactMatch = scoredBooks.FirstOrDefault(m => m.score >= 1.0).book;

        return new CoverAnalysisResponse
        {
            Analysis = new CoverAnalysisSummary
            {
                IsSuccess = true,
                ExtractedText = string.Join(" ", ocrResult.FilteredText.Select(w => w.Text))
            },
            MatchedBooks = scoredBooks.Take(ImageAnalysisConstants.MaxResultsPerResponse).Select(m => m.book).ToList(),
            ExactMatch = exactMatch
        };
    }

    private async Task<List<(Book book, double score)>> SearchForMatchingBooksAsync(CoverAnalysisResult ocrResult, string requestId)
    {
        if (ocrResult.FilteredText.Count == 0)
            return [];

        var currentWords = ocrResult.FilteredText.ToList();
        var scoredBooks = new List<(Book book, double score)>();

        for (int attempt = 0; attempt < ImageAnalysisConstants.MaxLookupRetries && scoredBooks.Count == 0; attempt++)
        {
            if (currentWords.Count == 0)
                break;

            scoredBooks = await SearchAttemptAsync(currentWords, requestId, attempt, ImageAnalysisConstants.MaxLookupRetries);

            if (scoredBooks.Count == 0 && attempt < ImageAnalysisConstants.MaxLookupRetries - 1)
            {
                var sharpenedWords = SharpenSearchWords(currentWords);
                if (sharpenedWords == currentWords)
                    break;

                currentWords = sharpenedWords;
                ocrResult.FilteredText = sharpenedWords;
            }
        }

        return scoredBooks;
    }

    private async Task<List<(Book book, double score)>> SearchAttemptAsync(
        List<ExtractedWord> currentWords, string requestId, int attempt, int maxRetries)
    {
        var searchText = string.Join(" ", currentWords.Select(w => w.Text));

        _logger.LogInformation(
            "Search attempt {Attempt}/{Max} [RequestId={RequestId}, FilteredWords={Words}, TextLength={Length}]",
            attempt + 1, maxRetries, requestId, currentWords.Count, searchText.Length);

        var ocrWords = BuildOcrWordSet(currentWords);
        var externalMatches = await _bookLookupService.SearchBooksByTextAsync(searchText);
        var scoredExternalMatches = ScoreAndFilterMatches(externalMatches, ocrWords);

        _logger.LogInformation(
            "Search results [RequestId={RequestId}, Attempt={Attempt}, RawResults={Raw}, FilteredResults={Filtered}]",
            requestId, attempt + 1, externalMatches.Count, scoredExternalMatches.Count);

        if (!scoredExternalMatches.Any())
            return [];

        return await MergeWithLocalBooksAsync(scoredExternalMatches, ocrWords);
    }

    private async Task<List<(Book book, double score)>> MergeWithLocalBooksAsync(
        List<BookLookupResult> scoredExternalMatches, HashSet<string> ocrWords)
    {
        var externalTitles = scoredExternalMatches.Select(m => m.Title.ToLower()).ToList();
        var externalAuthors = scoredExternalMatches.Select(m => m.Author.ToLower()).ToList();

        var localMatches = await _context.Books
            .Where(b => externalTitles.Contains(b.Title.ToLower()) ||
                        externalAuthors.Contains(b.Author.ToLower()))
            .ToListAsync();

        var allScoredBooks = new List<(Book book, double score, bool isLocal)>();

        foreach (var localBook in localMatches)
        {
            var score = CalculateWordMatchScore(localBook.Title, localBook.Author, ocrWords);
            if (score >= ImageAnalysisConstants.MinWordMatchThreshold)
                allScoredBooks.Add((localBook, score, true));
        }

        var localTitles = localMatches.Select(b => b.Title.ToLower()).ToHashSet();
        var externalId = -2;
        foreach (var ext in scoredExternalMatches)
        {
            if (!localTitles.Contains(ext.Title.ToLower()))
            {
                var externalBook = new Book
                {
                    Id = externalId--,
                    Title = ext.Title,
                    Author = ext.Author,
                    ExternalThumbnailUrl = ext.ThumbnailUrl
                };
                var score = CalculateWordMatchScore(ext.Title, ext.Author, ocrWords);
                allScoredBooks.Add((externalBook, score, false));
            }
        }

        return allScoredBooks
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.isLocal)
            .Select(x => (x.book, x.score))
            .ToList();
    }

    private static HashSet<string> BuildOcrWordSet(List<ExtractedWord> words) =>
        words
            .Select(w => w.Text.ToLower().Trim(',', '.', '!', '?', ';', ':'))
            .Where(w => w.Length > 2)
            .ToHashSet();

    private List<BookLookupResult> ScoreAndFilterMatches(
        List<BookLookupResult> candidates, HashSet<string> ocrWords) =>
        candidates
            .Select(match => new
            {
                Book = match,
                Score = CalculateWordMatchScore(match.Title, match.Author, ocrWords)
            })
            .Where(m => m.Score >= ImageAnalysisConstants.MinWordMatchThreshold)
            .OrderByDescending(m => m.Score)
            .Select(m => m.Book)
            .ToList();

    private static double CalculateWordMatchScore(string title, string author, HashSet<string> ocrWords)
    {
        if (ocrWords.Count == 0)
            return 0;

        var bookWords = title
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Concat(author.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(w => w.ToLower().Trim(',', '.', '!', '?', ';', ':'))
            .Where(w => w.Length > 2)
            .ToList();

        if (bookWords.Count == 0)
            return 0;

        var matchCount = bookWords.Count(w => ocrWords.Contains(w));
        return (double)matchCount / bookWords.Count;
    }

    /// <summary>
    /// Sharpens OCR search words by exploiting the visual hierarchy of book covers.
    /// Groups words into height tiers, finds the largest proportional gap between tiers,
    /// and discards everything below the gap — keeping only the most prominent text
    /// (typically title and author).
    /// </summary>
    private List<ExtractedWord> SharpenSearchWords(List<ExtractedWord> searchWords)
    {
        if (searchWords.Count < ImageAnalysisConstants.SharpenMinWordsRequired)
            return searchWords;

        if (searchWords.All(w => w.Height <= 0))
            return searchWords;

        var tiers = BuildHeightTiers(searchWords);

        if (tiers.Count < ImageAnalysisConstants.SharpenMinTiersRequired)
            return searchWords;

        var (bestGapIndex, bestGapRatio) = FindLargestHeightGap(tiers);

        if (bestGapRatio < ImageAnalysisConstants.SharpenMinGapThreshold)
            return searchWords;

        var survivingWords = tiers
            .Take(bestGapIndex + 1)
            .SelectMany(t => t.Words)
            .ToHashSet();

        if (survivingWords.Count < ImageAnalysisConstants.SharpenMinWordsAfterCut)
            return searchWords;

        return searchWords.Where(w => survivingWords.Contains(w)).ToList();
    }

    private static (int bestGapIndex, double bestGapRatio) FindLargestHeightGap(List<HeightTier> tiers)
    {
        int bestGapIndex = -1;
        double bestGapRatio = 0;

        for (int i = 0; i < tiers.Count - 1; i++)
        {
            var upperHeight = tiers[i].RepresentativeHeight;
            var lowerHeight = tiers[i + 1].RepresentativeHeight;
            var gapRatio = (upperHeight - lowerHeight) / upperHeight;

            if (gapRatio > bestGapRatio)
            {
                bestGapRatio = gapRatio;
                bestGapIndex = i;
            }
        }

        return (bestGapIndex, bestGapRatio);
    }

    /// <summary>
    /// Groups words into tiers of similar visual height. Words are sorted by height
    /// descending, and a new tier starts when a word's height drops below the tolerance
    /// threshold relative to the current tier's representative height.
    /// </summary>
    private static List<HeightTier> BuildHeightTiers(List<ExtractedWord> words)
    {
        var sorted = words
            .Where(w => w.Height > 0)
            .OrderByDescending(w => w.Height)
            .ToList();

        var tiers = new List<HeightTier>();
        HeightTier? currentTier = null;

        foreach (var word in sorted)
        {
            if (currentTier == null ||
                word.Height < currentTier.RepresentativeHeight * (1 - ImageAnalysisConstants.SharpenTierGroupingTolerance))
            {
                currentTier = new HeightTier { RepresentativeHeight = word.Height };
                tiers.Add(currentTier);
            }

            currentTier.Words.Add(word);
        }

        return tiers;
    }

    private static CoverAnalysisResponse FailureResponse(string? errorMessage) => new()
    {
        Analysis = new CoverAnalysisSummary
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        }
    };

    private class HeightTier
    {
        public double RepresentativeHeight { get; init; }
        public List<ExtractedWord> Words { get; } = new();
    }
}
