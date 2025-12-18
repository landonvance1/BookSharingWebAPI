using BookSharingApp.Services;
using FluentAssertions;

namespace BookSharingApp.Tests.Services
{
    public class InMemoryRateLimiterTests
    {
        // Shared base class for all nested test classes
        public abstract class InMemoryRateLimiterTestBase : IDisposable
        {
            protected InMemoryRateLimiter RateLimiter;

            protected InMemoryRateLimiterTestBase()
            {
                RateLimiter = new InMemoryRateLimiter();
            }

            public void Dispose()
            {
                RateLimiter?.Dispose();
            }
        }

        public class ConfigureLimitTests : InMemoryRateLimiterTestBase
        {
            [Fact]
            public void ConfigureLimit_WithNewLimit_ConfiguresSuccessfully()
            {
                // Arrange
                var limitName = "test-limit";
                var maxTokens = 10;
                var window = TimeSpan.FromSeconds(1);

                // Act
                RateLimiter.ConfigureLimit(limitName, maxTokens, window);

                // Assert - Verify configuration by attempting to consume
                var key = $"{limitName}:user:test-user";
                var result = RateLimiter.TryConsumeAsync(key).Result;
                result.Should().BeTrue();
            }

            [Fact]
            public void ConfigureLimit_WithExistingLimit_UpdatesConfiguration()
            {
                // Arrange
                var limitName = "test-limit";
                RateLimiter.ConfigureLimit(limitName, 5, TimeSpan.FromSeconds(1));

                // Act - Update configuration
                var newMaxTokens = 20;
                var newWindow = TimeSpan.FromSeconds(2);
                RateLimiter.ConfigureLimit(limitName, newMaxTokens, newWindow);

                // Assert - Verify updated configuration allows more consumption
                var key = $"{limitName}:user:test-user";
                for (int i = 0; i < 20; i++)
                {
                    var result = RateLimiter.TryConsumeAsync(key).Result;
                    result.Should().BeTrue($"consumption {i + 1} should succeed");
                }

                // 21st attempt should fail
                var finalResult = RateLimiter.TryConsumeAsync(key).Result;
                finalResult.Should().BeFalse();
            }

            [Fact]
            public void ConfigureLimit_WithMultipleLimits_ConfiguresIndependently()
            {
                // Arrange & Act
                RateLimiter.ConfigureLimit("limit1", 5, TimeSpan.FromSeconds(1));
                RateLimiter.ConfigureLimit("limit2", 10, TimeSpan.FromSeconds(1));

                // Assert
                var key1 = "limit1:user:test-user";
                var key2 = "limit2:user:test-user";

                // limit1 should allow 5 consumptions
                for (int i = 0; i < 5; i++)
                {
                    RateLimiter.TryConsumeAsync(key1).Result.Should().BeTrue();
                }
                RateLimiter.TryConsumeAsync(key1).Result.Should().BeFalse();

                // limit2 should allow 10 consumptions
                for (int i = 0; i < 10; i++)
                {
                    RateLimiter.TryConsumeAsync(key2).Result.Should().BeTrue();
                }
                RateLimiter.TryConsumeAsync(key2).Result.Should().BeFalse();
            }
        }

        public class TryConsumeAsyncTests : InMemoryRateLimiterTestBase
        {
            [Fact]
            public async Task TryConsumeAsync_WithValidKey_ConsumesTokenSuccessfully()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 10, TimeSpan.FromSeconds(1));
                var key = "test-limit:user:user-1";

                // Act
                var result = await RateLimiter.TryConsumeAsync(key);

                // Assert
                result.Should().BeTrue();
            }

            [Fact]
            public async Task TryConsumeAsync_WithMultipleTokens_ConsumesCorrectly()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 10, TimeSpan.FromSeconds(1));
                var key = "test-limit:user:user-1";

                // Act - Consume 5 tokens
                var result = await RateLimiter.TryConsumeAsync(key, 5);

                // Assert
                result.Should().BeTrue();

                // Should be able to consume 5 more
                var result2 = await RateLimiter.TryConsumeAsync(key, 5);
                result2.Should().BeTrue();

                // 11th token should fail
                var result3 = await RateLimiter.TryConsumeAsync(key, 1);
                result3.Should().BeFalse();
            }

            [Fact]
            public async Task TryConsumeAsync_WhenLimitExceeded_ReturnsFalse()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 3, TimeSpan.FromSeconds(1));
                var key = "test-limit:user:user-1";

                // Act - Consume all tokens
                await RateLimiter.TryConsumeAsync(key); // 1
                await RateLimiter.TryConsumeAsync(key); // 2
                await RateLimiter.TryConsumeAsync(key); // 3

                var result = await RateLimiter.TryConsumeAsync(key); // 4th attempt

                // Assert
                result.Should().BeFalse();
            }

            [Fact]
            public async Task TryConsumeAsync_WhenLimitExceeded_DoesNotConsumeTokens()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 2, TimeSpan.FromSeconds(1));
                var key = "test-limit:user:user-1";

                // Act - Consume all tokens
                await RateLimiter.TryConsumeAsync(key); // 1
                await RateLimiter.TryConsumeAsync(key); // 2

                // Try to consume when limit exceeded
                await RateLimiter.TryConsumeAsync(key); // Should fail
                await RateLimiter.TryConsumeAsync(key); // Should still fail

                // Wait for window to expire
                await Task.Delay(1100);

                // Assert - Should be able to consume again (tokens reset)
                var result = await RateLimiter.TryConsumeAsync(key);
                result.Should().BeTrue();
            }

            [Fact]
            public async Task TryConsumeAsync_AfterWindowExpires_ResetsTokens()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 3, TimeSpan.FromMilliseconds(100));
                var key = "test-limit:user:user-1";

                // Act - Consume all tokens
                await RateLimiter.TryConsumeAsync(key);
                await RateLimiter.TryConsumeAsync(key);
                await RateLimiter.TryConsumeAsync(key);

                // Verify limit reached
                var resultBeforeReset = await RateLimiter.TryConsumeAsync(key);
                resultBeforeReset.Should().BeFalse();

                // Wait for window to expire
                await Task.Delay(150);

                // Try again after window expires
                var resultAfterReset = await RateLimiter.TryConsumeAsync(key);

                // Assert
                resultAfterReset.Should().BeTrue();
            }

            [Fact]
            public async Task TryConsumeAsync_WithInvalidKeyFormat_ReturnsFalse()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 10, TimeSpan.FromSeconds(1));
                var invalidKey = "invalidkey"; // No colon separator

                // Act
                var result = await RateLimiter.TryConsumeAsync(invalidKey);

                // Assert
                result.Should().BeFalse();
            }

            [Fact]
            public async Task TryConsumeAsync_WithUnconfiguredLimit_ReturnsFalse()
            {
                // Arrange
                var key = "unconfigured-limit:user:user-1";

                // Act
                var result = await RateLimiter.TryConsumeAsync(key);

                // Assert
                result.Should().BeFalse();
            }

            [Fact]
            public async Task TryConsumeAsync_WithWindowExpiringDuringConsumption_HandlesCorrectly()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 5, TimeSpan.FromMilliseconds(100));
                var key = "test-limit:user:user-1";

                // Act - Consume some tokens
                await RateLimiter.TryConsumeAsync(key); // 1
                await RateLimiter.TryConsumeAsync(key); // 2

                // Wait for window to expire
                await Task.Delay(150);

                // Consume after expiration - should get fresh bucket
                var result = await RateLimiter.TryConsumeAsync(key);

                // Assert
                result.Should().BeTrue();

                // Should be able to consume 4 more (total 5 in new window)
                for (int i = 0; i < 4; i++)
                {
                    var consumeResult = await RateLimiter.TryConsumeAsync(key);
                    consumeResult.Should().BeTrue();
                }

                // 6th consumption should fail
                var finalResult = await RateLimiter.TryConsumeAsync(key);
                finalResult.Should().BeFalse();
            }

            [Fact]
            public async Task TryConsumeAsync_TracksRemainingTokensCorrectly()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 5, TimeSpan.FromSeconds(1));
                var key = "test-limit:user:user-1";

                // Act & Assert - Consume tokens one by one
                for (int i = 0; i < 5; i++)
                {
                    var result = await RateLimiter.TryConsumeAsync(key);
                    result.Should().BeTrue($"consumption {i + 1} should succeed");
                }

                // 6th attempt should fail
                var finalResult = await RateLimiter.TryConsumeAsync(key);
                finalResult.Should().BeFalse();
            }

            [Fact]
            public async Task TryConsumeAsync_WithConcurrentConsumptionSameBucket_HandlesCorrectly()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 10, TimeSpan.FromSeconds(1));
                var key = "test-limit:user:user-1";

                // Act - Simulate 10 concurrent consumptions on same bucket
                var tasks = Enumerable.Range(0, 10)
                    .Select(_ => RateLimiter.TryConsumeAsync(key))
                    .ToArray();

                var results = await Task.WhenAll(tasks);

                // Assert - All should succeed (10 tokens available)
                results.Should().AllSatisfy(r => r.Should().BeTrue());

                // 11th attempt should fail
                var finalResult = await RateLimiter.TryConsumeAsync(key);
                finalResult.Should().BeFalse();
            }

            [Fact]
            public async Task TryConsumeAsync_WithConcurrentConsumptionDifferentBuckets_HandlesCorrectly()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 5, TimeSpan.FromSeconds(1));

                // Act - Simulate concurrent consumptions on different buckets
                var tasks = Enumerable.Range(1, 10)
                    .Select(i => RateLimiter.TryConsumeAsync($"test-limit:user:user-{i}"))
                    .ToArray();

                var results = await Task.WhenAll(tasks);

                // Assert - All should succeed (different buckets, each has 5 tokens)
                results.Should().AllSatisfy(r => r.Should().BeTrue());
            }
        }

        public class DisposalTests : InMemoryRateLimiterTestBase
        {
            [Fact]
            public void Dispose_DisposesTimerCorrectly()
            {
                // Arrange
                RateLimiter.ConfigureLimit("test-limit", 10, TimeSpan.FromSeconds(1));

                // Act
                RateLimiter.Dispose();

                // Assert - No exception should be thrown
                // Timer disposal is tested by ensuring no ObjectDisposedException is thrown
                // We can't directly verify timer disposal, but we ensure the Dispose call completes
                Action act = () => RateLimiter.Dispose();
                act.Should().NotThrow();
            }
        }
    }
}
