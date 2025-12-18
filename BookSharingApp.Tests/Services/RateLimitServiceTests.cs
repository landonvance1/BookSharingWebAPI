using BookSharingApp.Services;
using FluentAssertions;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class RateLimitServiceTests
    {
        // Shared base class for all nested test classes
        public abstract class RateLimitServiceTestBase : IDisposable
        {
            protected readonly Mock<IRateLimiter> RateLimiterMock;
            protected readonly RateLimitService RateLimitService;

            protected RateLimitServiceTestBase()
            {
                RateLimiterMock = new Mock<IRateLimiter>();
                RateLimitService = new RateLimitService(RateLimiterMock.Object);
            }

            public void Dispose()
            {
                // Cleanup if needed
            }
        }

        public class TryConsumeForUserAsyncTests : RateLimitServiceTestBase
        {
            [Fact]
            public async Task TryConsumeForUserAsync_WithValidUserId_CreatesCorrectKeyFormat()
            {
                // Arrange
                var limitName = "chat-message";
                var userId = "user-123";
                var expectedKey = $"{limitName}:user:{userId}";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                await RateLimitService.TryConsumeForUserAsync(limitName, userId);

                // Assert
                RateLimiterMock.Verify(
                    r => r.TryConsumeAsync(expectedKey, 1),
                    Times.Once
                );
            }

            [Fact]
            public async Task TryConsumeForUserAsync_WithNullUserId_ThrowsArgumentException()
            {
                // Arrange
                var limitName = "test-limit";
                string? userId = null;

                // Act
                Func<Task> act = async () => await RateLimitService.TryConsumeForUserAsync(limitName, userId!);

                // Assert
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithParameterName("userId");
            }

            [Fact]
            public async Task TryConsumeForUserAsync_WithEmptyUserId_ThrowsArgumentException()
            {
                // Arrange
                var limitName = "test-limit";
                var userId = "";

                // Act
                Func<Task> act = async () => await RateLimitService.TryConsumeForUserAsync(limitName, userId);

                // Assert
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithParameterName("userId");
            }

            [Fact]
            public async Task TryConsumeForUserAsync_DelegatesToRateLimiter()
            {
                // Arrange
                var limitName = "test-limit";
                var userId = "user-123";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                await RateLimitService.TryConsumeForUserAsync(limitName, userId);

                // Assert
                RateLimiterMock.Verify(
                    r => r.TryConsumeAsync(It.IsAny<string>(), 1),
                    Times.Once
                );
            }

            [Fact]
            public async Task TryConsumeForUserAsync_WhenRateLimiterReturnsTrue_ReturnsTrue()
            {
                // Arrange
                var limitName = "test-limit";
                var userId = "user-123";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                var result = await RateLimitService.TryConsumeForUserAsync(limitName, userId);

                // Assert
                result.Should().BeTrue();
            }

            [Fact]
            public async Task TryConsumeForUserAsync_WhenRateLimiterReturnsFalse_ReturnsFalse()
            {
                // Arrange
                var limitName = "test-limit";
                var userId = "user-123";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(false);

                // Act
                var result = await RateLimitService.TryConsumeForUserAsync(limitName, userId);

                // Assert
                result.Should().BeFalse();
            }
        }

        public class TryConsumeForIpAsyncTests : RateLimitServiceTestBase
        {
            [Fact]
            public async Task TryConsumeForIpAsync_WithValidIpAddress_CreatesCorrectKeyFormat()
            {
                // Arrange
                var limitName = "api-request";
                var ipAddress = "192.168.1.1";
                var expectedKey = $"{limitName}:ip:{ipAddress}";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                await RateLimitService.TryConsumeForIpAsync(limitName, ipAddress);

                // Assert
                RateLimiterMock.Verify(
                    r => r.TryConsumeAsync(expectedKey, 1),
                    Times.Once
                );
            }

            [Fact]
            public async Task TryConsumeForIpAsync_WithNullIpAddress_ThrowsArgumentException()
            {
                // Arrange
                var limitName = "test-limit";
                string? ipAddress = null;

                // Act
                Func<Task> act = async () => await RateLimitService.TryConsumeForIpAsync(limitName, ipAddress!);

                // Assert
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithParameterName("ipAddress");
            }

            [Fact]
            public async Task TryConsumeForIpAsync_WithEmptyIpAddress_ThrowsArgumentException()
            {
                // Arrange
                var limitName = "test-limit";
                var ipAddress = "";

                // Act
                Func<Task> act = async () => await RateLimitService.TryConsumeForIpAsync(limitName, ipAddress);

                // Assert
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithParameterName("ipAddress");
            }

            [Fact]
            public async Task TryConsumeForIpAsync_DelegatesToRateLimiter()
            {
                // Arrange
                var limitName = "test-limit";
                var ipAddress = "192.168.1.1";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                await RateLimitService.TryConsumeForIpAsync(limitName, ipAddress);

                // Assert
                RateLimiterMock.Verify(
                    r => r.TryConsumeAsync(It.IsAny<string>(), 1),
                    Times.Once
                );
            }

            [Fact]
            public async Task TryConsumeForIpAsync_WhenRateLimiterReturnsTrue_ReturnsTrue()
            {
                // Arrange
                var limitName = "test-limit";
                var ipAddress = "192.168.1.1";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                var result = await RateLimitService.TryConsumeForIpAsync(limitName, ipAddress);

                // Assert
                result.Should().BeTrue();
            }

            [Fact]
            public async Task TryConsumeForIpAsync_WhenRateLimiterReturnsFalse_ReturnsFalse()
            {
                // Arrange
                var limitName = "test-limit";
                var ipAddress = "192.168.1.1";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(false);

                // Act
                var result = await RateLimitService.TryConsumeForIpAsync(limitName, ipAddress);

                // Assert
                result.Should().BeFalse();
            }
        }

        public class TryConsumeGlobalAsyncTests : RateLimitServiceTestBase
        {
            [Fact]
            public async Task TryConsumeGlobalAsync_CreatesCorrectKeyFormat()
            {
                // Arrange
                var limitName = "global-api";
                var expectedKey = $"{limitName}:global";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                await RateLimitService.TryConsumeGlobalAsync(limitName);

                // Assert
                RateLimiterMock.Verify(
                    r => r.TryConsumeAsync(expectedKey, 1),
                    Times.Once
                );
            }

            [Fact]
            public async Task TryConsumeGlobalAsync_DelegatesToRateLimiter()
            {
                // Arrange
                var limitName = "test-limit";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                await RateLimitService.TryConsumeGlobalAsync(limitName);

                // Assert
                RateLimiterMock.Verify(
                    r => r.TryConsumeAsync(It.IsAny<string>(), 1),
                    Times.Once
                );
            }

            [Fact]
            public async Task TryConsumeGlobalAsync_WhenRateLimiterReturnsTrue_ReturnsTrue()
            {
                // Arrange
                var limitName = "test-limit";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(true);

                // Act
                var result = await RateLimitService.TryConsumeGlobalAsync(limitName);

                // Assert
                result.Should().BeTrue();
            }

            [Fact]
            public async Task TryConsumeGlobalAsync_WhenRateLimiterReturnsFalse_ReturnsFalse()
            {
                // Arrange
                var limitName = "test-limit";

                RateLimiterMock
                    .Setup(r => r.TryConsumeAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .ReturnsAsync(false);

                // Act
                var result = await RateLimitService.TryConsumeGlobalAsync(limitName);

                // Assert
                result.Should().BeFalse();
            }
        }
    }
}
