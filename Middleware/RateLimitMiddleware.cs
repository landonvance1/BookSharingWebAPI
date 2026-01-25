using BookSharingApp.Services;
using BookSharingApp.Common;
using System.Security.Claims;

namespace BookSharingApp.Middleware
{
    /// <summary>
    /// Middleware that enforces rate limiting on API requests to protect against abuse.
    /// </summary>
    public class RateLimitMiddleware
    {
        private const int RetryAfterSeconds = 60;

        private readonly RequestDelegate _next;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(RequestDelegate next, IRateLimitService rateLimitService, ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        /// <summary>
        /// Enforces a two-tier rate limiting strategy to protect the API from abuse.
        /// <para>
        /// Execution Order:
        /// <list type="number">
        ///   <item>Global IP-based limit (200 req/min): Prevents brute force from single IP</item>
        ///   <item>Global user-based limit (500 req/min): Prevents abuse from authenticated users</item>
        ///   <item>Endpoint-specific limit: Additional protection for sensitive endpoints (auth, chat)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Early termination: If any limit is exceeded, the request is immediately rejected with HTTP 429.
        /// This prevents unnecessary processing of rate-limited requests.
        /// </para>
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();

            // 1. Always check global IP-based rate limit
            if (ipAddress != null)
            {
                var ipLimitResult = await _rateLimitService.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, ipAddress);
                if (!ipLimitResult)
                {
                    _logger.LogWarning("Global IP rate limit exceeded for IP {IpAddress} on {Path}", ipAddress, context.Request.Path);
                    await WriteRateLimitExceededResponseAsync(context);
                    return;
                }
            }

            // 2. Check global user-based rate limit for authenticated requests
            if (userId != null)
            {
                var userLimitResult = await _rateLimitService.TryConsumeForUserAsync(RateLimitNames.GlobalApiUser, userId);
                if (!userLimitResult)
                {
                    _logger.LogWarning("Global user rate limit exceeded for user {UserId} on {Path}", userId, context.Request.Path);
                    await WriteRateLimitExceededResponseAsync(context);
                    return;
                }
            }

            // 3. Check endpoint-specific rate limit if present
            var endpoint = context.GetEndpoint();
            var rateLimitAttribute = endpoint?.Metadata.GetMetadata<RateLimitAttribute>();

            if (rateLimitAttribute != null)
            {
                var rateLimitResult = rateLimitAttribute.Scope switch
                {
                    RateLimitScope.User => userId != null && await _rateLimitService.TryConsumeForUserAsync(rateLimitAttribute.LimitName, userId),
                    RateLimitScope.Ip => ipAddress != null && await _rateLimitService.TryConsumeForIpAsync(rateLimitAttribute.LimitName, ipAddress),
                    RateLimitScope.Global => await _rateLimitService.TryConsumeGlobalAsync(rateLimitAttribute.LimitName),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (!rateLimitResult)
                {
                    _logger.LogWarning("Endpoint rate limit '{LimitName}' exceeded for user {UserId} on {Path}",
                        rateLimitAttribute.LimitName, userId ?? "anonymous", context.Request.Path);
                    await WriteRateLimitExceededResponseAsync(context);
                    return;
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Writes a standardized HTTP 429 rate limit exceeded response with JSON body and Retry-After header.
        /// </summary>
        private static async Task WriteRateLimitExceededResponseAsync(HttpContext context)
        {
            context.Response.StatusCode = 429;
            context.Response.Headers["Retry-After"] = RetryAfterSeconds.ToString();
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "Rate limit exceeded. Please try again later." });
        }
    }
}