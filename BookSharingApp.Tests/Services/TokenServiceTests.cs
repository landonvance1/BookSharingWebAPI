using BookSharingApp.Models;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BookSharingApp.Tests.Services
{
    public class TokenServiceTests
    {
        // Shared base class for all nested test classes
        public abstract class TokenServiceTestBase : IDisposable
        {
            protected const string TestJwtKey = "ThisIsATestJwtKeyForUnitTestingPurposes12345678";
            protected readonly Mock<IConfiguration> ConfigurationMock;
            protected readonly JwtSecurityTokenHandler TokenHandler;

            protected TokenServiceTestBase()
            {
                ConfigurationMock = new Mock<IConfiguration>();
                TokenHandler = new JwtSecurityTokenHandler();
            }

            protected void SetupConfiguration(string? jwtKey = null, string? jwtKeyFromEnv = null)
            {
                ConfigurationMock.Setup(c => c["JWT:Key"]).Returns(jwtKey);
                ConfigurationMock.Setup(c => c["JWT_KEY"]).Returns(jwtKeyFromEnv);
            }

            public void Dispose()
            {
                // Cleanup if needed
            }
        }

        public class GenerateAccessTokenTests : TokenServiceTestBase
        {
            [Fact]
            public void GenerateAccessToken_WithValidUser_CreatesValidJwtToken()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser(
                    id: "test-user-1",
                    firstName: "John",
                    lastName: "Doe",
                    email: "john.doe@test.com"
                );

                // Act
                var token = tokenService.GenerateAccessToken(user);

                // Assert
                token.Should().NotBeNullOrEmpty();
                var jwtToken = TokenHandler.ReadJwtToken(token);
                jwtToken.Should().NotBeNull();
            }

            [Fact]
            public void GenerateAccessToken_WithValidUser_ContainsAllRequiredClaims()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser(
                    id: "test-user-1",
                    firstName: "John",
                    lastName: "Doe",
                    email: "john.doe@test.com"
                );

                // Act
                var token = tokenService.GenerateAccessToken(user);

                // Assert
                var jwtToken = TokenHandler.ReadJwtToken(token);

                // JWT serializes claim types to short names, so we need to validate using GetPrincipalFromExpiredToken
                // which properly deserializes the claims with their full URIs
                var principal = tokenService.GetPrincipalFromExpiredToken(token);
                var claims = principal.Claims.ToList();

                claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
                claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
                claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.FullName);
                claims.Should().Contain(c => c.Type == "firstName" && c.Value == user.FirstName);
                claims.Should().Contain(c => c.Type == "lastName" && c.Value == user.LastName);
            }

            [Fact]
            public void GenerateAccessToken_WithValidUser_SetsExpirationTo24Hours()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser();
                var beforeGeneration = DateTime.UtcNow;

                // Act
                var token = tokenService.GenerateAccessToken(user);

                // Assert
                var jwtToken = TokenHandler.ReadJwtToken(token);
                var expectedExpiration = beforeGeneration.AddHours(24);

                jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
            }

            [Fact]
            public void GenerateAccessToken_WithValidUser_UsesHmacSha256Algorithm()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser();

                // Act
                var token = tokenService.GenerateAccessToken(user);

                // Assert
                var jwtToken = TokenHandler.ReadJwtToken(token);
                jwtToken.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
            }

            [Fact]
            public void GenerateAccessToken_WhenJwtKeyMissing_ThrowsInvalidOperationException()
            {
                // Arrange
                SetupConfiguration(jwtKey: null, jwtKeyFromEnv: null);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser();

                // Act
                Action act = () => tokenService.GenerateAccessToken(user);

                // Assert
                act.Should().Throw<InvalidOperationException>()
                    .WithMessage("JWT key not found in configuration");
            }

            [Fact]
            public void GenerateAccessToken_WhenUserEmailIsNull_IncludesEmptyStringInEmailClaim()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);

                // Create user with explicitly null email (TestDataBuilder provides default, so create manually)
                var user = new User
                {
                    Id = "test-user-null-email",
                    FirstName = "Test",
                    LastName = "User",
                    Email = null // Explicitly null
                };

                // Act
                var token = tokenService.GenerateAccessToken(user);

                // Assert
                var principal = tokenService.GetPrincipalFromExpiredToken(token);
                var emailClaim = principal.FindFirst(ClaimTypes.Email);

                emailClaim.Should().NotBeNull();
                emailClaim!.Value.Should().Be("");
            }
        }

        public class GenerateRefreshTokenTests : TokenServiceTestBase
        {
            [Fact]
            public void GenerateRefreshToken_GeneratesNonEmptyBase64String()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);

                // Act
                var refreshToken = tokenService.GenerateRefreshToken();

                // Assert
                refreshToken.Should().NotBeNullOrEmpty();

                // Verify it's valid Base64
                Action act = () => Convert.FromBase64String(refreshToken);
                act.Should().NotThrow();
            }

            [Fact]
            public void GenerateRefreshToken_GeneratesUniqueTokens()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);

                // Act
                var token1 = tokenService.GenerateRefreshToken();
                var token2 = tokenService.GenerateRefreshToken();
                var token3 = tokenService.GenerateRefreshToken();

                // Assert
                token1.Should().NotBe(token2);
                token1.Should().NotBe(token3);
                token2.Should().NotBe(token3);
            }

            [Fact]
            public void GenerateRefreshToken_GeneratesExactly44Characters()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);

                // Act
                var refreshToken = tokenService.GenerateRefreshToken();

                // Assert
                // 32 bytes encoded as Base64 = 44 characters
                refreshToken.Length.Should().Be(44);
            }

            [Fact]
            public void GenerateRefreshToken_ContainsOnlyValidBase64Characters()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);

                // Act
                var refreshToken = tokenService.GenerateRefreshToken();

                // Assert
                // Valid Base64 characters: A-Z, a-z, 0-9, +, /, =
                refreshToken.Should().MatchRegex(@"^[A-Za-z0-9+/=]+$");
            }
        }

        public class GetPrincipalFromExpiredTokenTests : TokenServiceTestBase
        {
            [Fact]
            public void GetPrincipalFromExpiredToken_WithValidToken_ReturnsClaimsPrincipal()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser(
                    id: "test-user-1",
                    firstName: "John",
                    lastName: "Doe",
                    email: "john.doe@test.com"
                );
                var token = tokenService.GenerateAccessToken(user);

                // Act
                var principal = tokenService.GetPrincipalFromExpiredToken(token);

                // Assert
                principal.Should().NotBeNull();
                principal.Should().BeOfType<ClaimsPrincipal>();
            }

            [Fact]
            public void GetPrincipalFromExpiredToken_WithValidToken_ExtractsAllOriginalClaims()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser(
                    id: "test-user-1",
                    firstName: "John",
                    lastName: "Doe",
                    email: "john.doe@test.com"
                );
                var token = tokenService.GenerateAccessToken(user);

                // Act
                var principal = tokenService.GetPrincipalFromExpiredToken(token);

                // Assert
                var claims = principal.Claims.ToList();
                claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
                claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
                claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.FullName);
                claims.Should().Contain(c => c.Type == "firstName" && c.Value == user.FirstName);
                claims.Should().Contain(c => c.Type == "lastName" && c.Value == user.LastName);
            }

            [Fact]
            public void GetPrincipalFromExpiredToken_WithInvalidSignature_ThrowsSecurityTokenException()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser();
                var token = tokenService.GenerateAccessToken(user);

                // Tamper with the token by changing the signature
                var parts = token.Split('.');
                var tamperedToken = $"{parts[0]}.{parts[1]}.InvalidSignature";

                // Act
                Action act = () => tokenService.GetPrincipalFromExpiredToken(tamperedToken);

                // Assert
                act.Should().Throw<SecurityTokenException>();
            }

            [Fact]
            public void GetPrincipalFromExpiredToken_WithWrongAlgorithm_ThrowsSecurityTokenException()
            {
                // Arrange
                // Use a longer key for HS384 (requires at least 48 bytes)
                var longerKey = "ThisIsAMuchLongerJwtKeyForHS384AlgorithmTesting1234567890123456789012345678901234567890";
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);

                // Create a token with a different algorithm (HS384 instead of HS256)
                var key = Encoding.ASCII.GetBytes(longerKey);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test-user") }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha384 // Different algorithm
                    )
                };
                var handler = new JwtSecurityTokenHandler();
                var token = handler.WriteToken(handler.CreateToken(tokenDescriptor));

                // Act
                Action act = () => tokenService.GetPrincipalFromExpiredToken(token);

                // Assert - Token validation should fail because algorithm doesn't match
                act.Should().Throw<SecurityTokenException>();
            }

            [Fact]
            public void GetPrincipalFromExpiredToken_WithMalformedToken_ThrowsException()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var malformedToken = "this.is.not.a.valid.jwt.token";

                // Act
                Action act = () => tokenService.GetPrincipalFromExpiredToken(malformedToken);

                // Assert
                act.Should().Throw<Exception>();
            }

            [Fact]
            public void GetPrincipalFromExpiredToken_WhenJwtKeyMissing_ThrowsInvalidOperationException()
            {
                // Arrange
                SetupConfiguration(jwtKey: null, jwtKeyFromEnv: null);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var dummyToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";

                // Act
                Action act = () => tokenService.GetPrincipalFromExpiredToken(dummyToken);

                // Assert
                act.Should().Throw<InvalidOperationException>()
                    .WithMessage("JWT key not found in configuration");
            }

            [Fact]
            public void GetPrincipalFromExpiredToken_WithExpiredToken_StillValidatesSuccessfully()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);

                // Create an already-expired token
                var key = Encoding.ASCII.GetBytes(TestJwtKey);
                var user = TestDataBuilder.CreateUser(id: "expired-user");
                var now = DateTime.UtcNow;
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Email, user.Email!)
                    }),
                    NotBefore = now.AddHours(-2), // Set NotBefore before Expires
                    Expires = now.AddHours(-1), // Already expired
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature
                    )
                };
                var handler = new JwtSecurityTokenHandler();
                var expiredToken = handler.WriteToken(handler.CreateToken(tokenDescriptor));

                // Act
                var principal = tokenService.GetPrincipalFromExpiredToken(expiredToken);

                // Assert
                principal.Should().NotBeNull();
                principal.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
            }
        }

        public class EdgeCaseAndSecurityTests : TokenServiceTestBase
        {
            [Fact]
            public void GenerateAccessToken_CanBeValidatedImmediatelyAfterGeneration()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser(id: "validation-test");

                // Act
                var token = tokenService.GenerateAccessToken(user);
                var principal = tokenService.GetPrincipalFromExpiredToken(token);

                // Assert
                principal.Should().NotBeNull();
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                userIdClaim.Should().NotBeNull();
                userIdClaim!.Value.Should().Be(user.Id);
            }

            [Fact]
            public void TokenService_WithJwtKeyEnvironmentVariable_WorksCorrectly()
            {
                // Arrange
                SetupConfiguration(jwtKey: null, jwtKeyFromEnv: TestJwtKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser();

                // Act
                var token = tokenService.GenerateAccessToken(user);

                // Assert
                token.Should().NotBeNullOrEmpty();
                var jwtToken = TokenHandler.ReadJwtToken(token);
                jwtToken.Should().NotBeNull();
            }

            [Fact]
            public void TokenService_WithJwtKeyFromAppSettings_WorksCorrectly()
            {
                // Arrange
                SetupConfiguration(jwtKey: TestJwtKey, jwtKeyFromEnv: null);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser();

                // Act
                var token = tokenService.GenerateAccessToken(user);

                // Assert
                token.Should().NotBeNullOrEmpty();
                var jwtToken = TokenHandler.ReadJwtToken(token);
                jwtToken.Should().NotBeNull();
            }

            [Fact]
            public void TokenService_PrioritizesJwtKeyEnvironmentVariableOverAppSettings()
            {
                // Arrange
                var envKey = "EnvironmentKeyForTesting123456789012";
                var appSettingsKey = "AppSettingsKeyForTesting123456789012";

                SetupConfiguration(jwtKey: appSettingsKey, jwtKeyFromEnv: envKey);
                var tokenService = new TokenService(ConfigurationMock.Object);
                var user = TestDataBuilder.CreateUser(id: "priority-test");

                // Act
                var token = tokenService.GenerateAccessToken(user);

                // Assert - Token should be validatable with env key but not with appsettings key
                var key = Encoding.ASCII.GetBytes(envKey);
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false
                };

                var handler = new JwtSecurityTokenHandler();
                Action act = () => handler.ValidateToken(token, tokenValidationParameters, out _);
                act.Should().NotThrow();
            }
        }
    }
}
