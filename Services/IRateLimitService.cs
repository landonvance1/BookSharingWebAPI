namespace BookSharingApp.Services
{
    public interface IRateLimitService
    {
        Task<bool> TryConsumeForUserAsync(string limitName, string userId);
        Task<bool> TryConsumeForIpAsync(string limitName, string ipAddress);
        Task<bool> TryConsumeGlobalAsync(string limitName);
    }
}