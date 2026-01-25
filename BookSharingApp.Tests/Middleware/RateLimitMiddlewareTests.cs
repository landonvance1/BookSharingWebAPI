using BookSharingApp.Common;
using BookSharingApp.Middleware;
using BookSharingApp.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Security.Claims;

namespace BookSharingApp.Tests.Middleware
{
    public class RateLimitMiddlewareTests
    {
        public abstract class RateLimitMiddlewareTestBase : IDisposable
        {
            protected readonly Mock<IRateLimitService> RateLimitServiceMock;
            protected readonly Mock<ILogger<RateLimitMiddleware>> LoggerMock;
            protected bool NextDelegateCalled;
            protected RequestDelegate NextDelegate;

            protected RateLimitMiddlewareTestBase()
            {
                RateLimitServiceMock = new Mock<IRateLimitService>();
                LoggerMock = new Mock<ILogger<RateLimitMiddleware>>();
                NextDelegateCalled = false;
                NextDelegate = context =>
                {
                    NextDelegateCalled = true;
                    return Task.CompletedTask;
                };

                // Default setup: all rate limits pass
                SetupAllLimitsPass();
            }

            protected void SetupAllLimitsPass()
            {
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(true);
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForUserAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(true);
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeGlobalAsync(It.IsAny<string>()))
                    .ReturnsAsync(true);
            }

            protected HttpContext CreateHttpContext(
                string? userId = null,
                string? ipAddress = "192.168.1.1",
                Endpoint? endpoint = null)
            {
                var context = new DefaultHttpContext();

                // Set up IP address
                if (ipAddress != null)
                {
                    context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
                }

                // Set up user claims
                if (userId != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId)
                    };
                    var identity = new ClaimsIdentity(claims, "TestAuth");
                    context.User = new ClaimsPrincipal(identity);
                }

                // Set up endpoint
                if (endpoint != null)
                {
                    context.SetEndpoint(endpoint);
                }

                // Set up response body
                context.Response.Body = new MemoryStream();

                return context;
            }

            protected Endpoint CreateEndpointWithRateLimitAttribute(string limitName, RateLimitScope scope)
            {
                var metadata = new EndpointMetadataCollection(new RateLimitAttribute(limitName, scope));
                return new Endpoint(null, metadata, "TestEndpoint");
            }

            protected async Task<string> GetResponseBody(HttpContext context)
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(context.Response.Body);
                return await reader.ReadToEndAsync();
            }

            public void Dispose()
            {
                // Cleanup if needed
            }
        }

        public class GlobalIpLimitTests : RateLimitMiddlewareTestBase
        {
            [Fact]
            public async Task InvokeAsync_WithIpAddress_ChecksGlobalIpLimit()
            {
                // Arrange
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, "192.168.1.1"),
                    Times.Once);
            }

            [Fact]
            public async Task InvokeAsync_WhenGlobalIpLimitExceeded_Returns429()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                context.Response.StatusCode.Should().Be(429);
                NextDelegateCalled.Should().BeFalse();
            }

            [Fact]
            public async Task InvokeAsync_WhenGlobalIpLimitExceeded_WritesErrorMessage()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                var responseBody = await GetResponseBody(context);
                responseBody.Should().Contain("Rate limit exceeded");
            }

            [Fact]
            public async Task InvokeAsync_WithNullIpAddress_SkipsGlobalIpCheck()
            {
                // Arrange
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: null);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, It.IsAny<string>()),
                    Times.Never);
                NextDelegateCalled.Should().BeTrue();
            }
        }

        public class GlobalUserLimitTests : RateLimitMiddlewareTestBase
        {
            [Fact]
            public async Task InvokeAsync_WithAuthenticatedUser_ChecksGlobalUserLimit()
            {
                // Arrange
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForUserAsync(RateLimitNames.GlobalApiUser, "user-123"),
                    Times.Once);
            }

            [Fact]
            public async Task InvokeAsync_WhenGlobalUserLimitExceeded_Returns429()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForUserAsync(RateLimitNames.GlobalApiUser, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                context.Response.StatusCode.Should().Be(429);
                NextDelegateCalled.Should().BeFalse();
            }

            [Fact]
            public async Task InvokeAsync_WithAnonymousUser_SkipsGlobalUserCheck()
            {
                // Arrange
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: null, ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForUserAsync(RateLimitNames.GlobalApiUser, It.IsAny<string>()),
                    Times.Never);
                NextDelegateCalled.Should().BeTrue();
            }
        }

        public class EndpointSpecificLimitTests : RateLimitMiddlewareTestBase
        {
            [Fact]
            public async Task InvokeAsync_WithRateLimitAttribute_ChecksEndpointSpecificLimit()
            {
                // Arrange
                var endpoint = CreateEndpointWithRateLimitAttribute(RateLimitNames.AuthLogin, RateLimitScope.Ip);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForIpAsync(RateLimitNames.AuthLogin, "192.168.1.1"),
                    Times.Once);
            }

            [Fact]
            public async Task InvokeAsync_WithRateLimitAttribute_AlsoChecksGlobalLimits()
            {
                // Arrange
                var endpoint = CreateEndpointWithRateLimitAttribute(RateLimitNames.AuthLogin, RateLimitScope.Ip);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert - Both global IP and endpoint-specific limits are checked
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, "192.168.1.1"),
                    Times.Once);
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForIpAsync(RateLimitNames.AuthLogin, "192.168.1.1"),
                    Times.Once);
            }

            [Fact]
            public async Task InvokeAsync_WhenEndpointLimitExceeded_Returns429()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.AuthLogin, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var endpoint = CreateEndpointWithRateLimitAttribute(RateLimitNames.AuthLogin, RateLimitScope.Ip);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                context.Response.StatusCode.Should().Be(429);
                NextDelegateCalled.Should().BeFalse();
            }

            [Fact]
            public async Task InvokeAsync_WithUserScopedAttribute_ChecksUserLimit()
            {
                // Arrange
                var endpoint = CreateEndpointWithRateLimitAttribute(RateLimitNames.ChatSend, RateLimitScope.User);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForUserAsync(RateLimitNames.ChatSend, "user-123"),
                    Times.Once);
            }

            [Fact]
            public async Task InvokeAsync_WithGlobalScopedAttribute_ChecksGlobalLimit()
            {
                // Arrange
                var endpoint = CreateEndpointWithRateLimitAttribute("global-test", RateLimitScope.Global);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeGlobalAsync("global-test"),
                    Times.Once);
            }
        }

        public class RequestFlowTests : RateLimitMiddlewareTestBase
        {
            [Fact]
            public async Task InvokeAsync_WhenAllLimitsPass_CallsNextDelegate()
            {
                // Arrange
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                NextDelegateCalled.Should().BeTrue();
            }

            [Fact]
            public async Task InvokeAsync_WhenAllLimitsPass_DoesNotReturn429()
            {
                // Arrange
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                context.Response.StatusCode.Should().NotBe(429);
            }

            [Fact]
            public async Task InvokeAsync_ChecksLimitsInCorrectOrder_IpThenUserThenEndpoint()
            {
                // Arrange
                var callOrder = new List<string>();

                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, It.IsAny<string>()))
                    .Callback(() => callOrder.Add("GlobalApiIp"))
                    .ReturnsAsync(true);

                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForUserAsync(RateLimitNames.GlobalApiUser, It.IsAny<string>()))
                    .Callback(() => callOrder.Add("GlobalApiUser"))
                    .ReturnsAsync(true);

                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.AuthLogin, It.IsAny<string>()))
                    .Callback(() => callOrder.Add("AuthLogin"))
                    .ReturnsAsync(true);

                var endpoint = CreateEndpointWithRateLimitAttribute(RateLimitNames.AuthLogin, RateLimitScope.Ip);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                callOrder.Should().ContainInOrder("GlobalApiIp", "GlobalApiUser", "AuthLogin");
            }

            [Fact]
            public async Task InvokeAsync_WhenGlobalIpFails_DoesNotCheckSubsequentLimits()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var endpoint = CreateEndpointWithRateLimitAttribute(RateLimitNames.AuthLogin, RateLimitScope.Ip);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForUserAsync(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Never);
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForIpAsync(RateLimitNames.AuthLogin, It.IsAny<string>()),
                    Times.Never);
            }

            [Fact]
            public async Task InvokeAsync_WhenGlobalUserFails_DoesNotCheckEndpointLimit()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForUserAsync(RateLimitNames.GlobalApiUser, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var endpoint = CreateEndpointWithRateLimitAttribute(RateLimitNames.AuthLogin, RateLimitScope.Ip);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                RateLimitServiceMock.Verify(
                    r => r.TryConsumeForIpAsync(RateLimitNames.AuthLogin, It.IsAny<string>()),
                    Times.Never);
            }
        }

        public class ResponseFormatTests : RateLimitMiddlewareTestBase
        {
            [Fact]
            public async Task InvokeAsync_WhenRateLimitExceeded_ReturnsJsonContentType()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                context.Response.ContentType.Should().Contain("application/json");
            }

            [Fact]
            public async Task InvokeAsync_WhenRateLimitExceeded_IncludesRetryAfterHeader()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                context.Response.Headers.Should().ContainKey("Retry-After");
                context.Response.Headers["Retry-After"].ToString().Should().Be("60");
            }

            [Fact]
            public async Task InvokeAsync_WhenRateLimitExceeded_ReturnsJsonWithMessageProperty()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.GlobalApiIp, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                var responseBody = await GetResponseBody(context);
                responseBody.Should().Contain("\"message\"");
                responseBody.Should().Contain("Rate limit exceeded");
            }

            [Fact]
            public async Task InvokeAsync_WhenUserLimitExceeded_IncludesRetryAfterHeader()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForUserAsync(RateLimitNames.GlobalApiUser, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(userId: "user-123", ipAddress: "192.168.1.1");

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                context.Response.Headers.Should().ContainKey("Retry-After");
            }

            [Fact]
            public async Task InvokeAsync_WhenEndpointLimitExceeded_IncludesRetryAfterHeader()
            {
                // Arrange
                RateLimitServiceMock
                    .Setup(r => r.TryConsumeForIpAsync(RateLimitNames.AuthLogin, It.IsAny<string>()))
                    .ReturnsAsync(false);

                var endpoint = CreateEndpointWithRateLimitAttribute(RateLimitNames.AuthLogin, RateLimitScope.Ip);
                var middleware = new RateLimitMiddleware(NextDelegate, RateLimitServiceMock.Object, LoggerMock.Object);
                var context = CreateHttpContext(ipAddress: "192.168.1.1", endpoint: endpoint);

                // Act
                await middleware.InvokeAsync(context);

                // Assert
                context.Response.Headers.Should().ContainKey("Retry-After");
            }
        }
    }
}
