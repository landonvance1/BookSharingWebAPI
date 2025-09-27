using BookSharingApp.Services;
using BookSharingApp.Common;
using System.Security.Claims;

namespace BookSharingApp.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(RequestDelegate next, IRateLimitService rateLimitService, ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if endpoint has rate limiting
            var endpoint = context.GetEndpoint();
            var rateLimitAttribute = endpoint?.Metadata.GetMetadata<RateLimitAttribute>();

            if (rateLimitAttribute != null)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();

                var rateLimitResult = rateLimitAttribute.Scope switch
                {
                    RateLimitScope.User => await _rateLimitService.TryConsumeForUserAsync(rateLimitAttribute.LimitName, userId),
                    RateLimitScope.Ip => ipAddress != null && await _rateLimitService.TryConsumeForIpAsync(rateLimitAttribute.LimitName, ipAddress),
                    RateLimitScope.Global => await _rateLimitService.TryConsumeGlobalAsync(rateLimitAttribute.LimitName),
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (!rateLimitResult)
                {
                    _logger.LogWarning("Rate limit exceeded for user {UserId} on {Path}", userId, context.Request.Path);

                    context.Response.StatusCode = 429;
                    await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                    return;
                }
            }

            await _next(context);
        }

    }
}