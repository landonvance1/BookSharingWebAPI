using BookSharingApp.Models;

namespace BookSharingWebAPI.Models;

public class CoverAnalysisResponse
{
    public CoverAnalysisSummary Analysis { get; set; } = new();
    public List<Book> MatchedBooks { get; set; } = [];
    public Book? ExactMatch { get; set; }
}
