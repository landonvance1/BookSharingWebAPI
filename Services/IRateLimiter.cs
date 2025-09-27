namespace BookSharingApp.Services
{
    public interface IRateLimiter
    {
        Task<bool> TryConsumeAsync(string key, int tokens = 1);
        void ConfigureLimit(string limitName, int maxTokens, TimeSpan window);
    }
}