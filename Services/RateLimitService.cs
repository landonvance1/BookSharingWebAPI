namespace BookSharingApp.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IRateLimiter _rateLimiter;

        public RateLimitService(IRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }

        public async Task<bool> TryConsumeForUserAsync(string limitName, string userId)
        {
            ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));

            var key = $"{limitName}:user:{userId}";
            return await _rateLimiter.TryConsumeAsync(key);
        }

        public async Task<bool> TryConsumeForIpAsync(string limitName, string ipAddress)
        {
            ArgumentException.ThrowIfNullOrEmpty(ipAddress, nameof(ipAddress));

            var key = $"{limitName}:ip:{ipAddress}";
            return await _rateLimiter.TryConsumeAsync(key);
        }

        public async Task<bool> TryConsumeGlobalAsync(string limitName)
        {
            var key = $"{limitName}:global";
            return await _rateLimiter.TryConsumeAsync(key);
        }
    }
}