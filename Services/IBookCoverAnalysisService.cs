using BookSharingWebAPI.Models;

namespace BookSharingWebAPI.Services;

public interface IBookCoverAnalysisService
{
    Task<CoverAnalysisResponse> AnalyzeCoverAsync(Stream imageStream, string contentType, string requestId, CancellationToken cancellationToken = default);
}
