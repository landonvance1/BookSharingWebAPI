using System.Collections.Concurrent;

namespace BookSharingApp.Services
{
    public class InMemoryRateLimiter : IRateLimiter
    {
        private readonly ConcurrentDictionary<string, RateLimitEntry> _entries = new();
        private readonly ConcurrentDictionary<string, RateLimitConfig> _limits = new();
        private readonly Timer _cleanupTimer;

        public InMemoryRateLimiter()
        {
            // Cleanup expired entries every minute
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public void ConfigureLimit(string limitName, int maxTokens, TimeSpan window)
        {
            _limits[limitName] = new RateLimitConfig(maxTokens, window);
        }

        public async Task<bool> TryConsumeAsync(string key, int tokens = 1)
        {
            await Task.CompletedTask; // Make it async for interface consistency

            var parts = key.Split(':');
            if (parts.Length < 2) return false;

            var limitName = parts[0];
            if (!_limits.TryGetValue(limitName, out var limit)) return false;

            var now = DateTime.UtcNow;
            var entry = _entries.AddOrUpdate(key,
                k => new RateLimitEntry(limit.MaxTokens - tokens, now.Add(limit.Window)),
                (k, existing) => {
                    if (now > existing.ResetTime)
                    {
                        // Window has expired, reset
                        return new RateLimitEntry(limit.MaxTokens - tokens, now.Add(limit.Window));
                    }

                    if (existing.RemainingTokens >= tokens)
                    {
                        // Consume tokens
                        return new RateLimitEntry(existing.RemainingTokens - tokens, existing.ResetTime);
                    }

                    // Not enough tokens
                    return existing;
                });

            return entry.RemainingTokens > 0;
        }

        private void CleanupExpiredEntries(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _entries
                .Where(kvp => now > kvp.Value.ResetTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _entries.TryRemove(key, out _);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }

    public record RateLimitConfig(int MaxTokens, TimeSpan Window);
    public record RateLimitEntry(int RemainingTokens, DateTime ResetTime);
}