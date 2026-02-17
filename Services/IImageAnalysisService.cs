using BookSharingWebAPI.Models;

namespace BookSharingWebAPI.Services;

public interface IImageAnalysisService
{
    Task<CoverAnalysisResult> AnalyzeCoverImageAsync(Stream imageStream, string contentType, CancellationToken cancellationToken = default);
}
