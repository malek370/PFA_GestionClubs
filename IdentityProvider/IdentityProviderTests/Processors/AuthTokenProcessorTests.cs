using IdentityProvider.Entities;
using IdentityProvider.Options;
using IdentityProvider.Processors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Security.Claims;
using Xunit;
using System.IdentityModel.Tokens.Jwt;


namespace IdentityProviderTests.Processors
{
    public class AuthTokenProcessorTests
    {
        private readonly JwtOptions _jwtOptions;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly AuthTokenProcessor _sut;

        public AuthTokenProcessorTests()
        {
            _jwtOptions = new JwtOptions
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                Secret = "SuperSecretKeyThatIsAtLeast32CharactersLong!",
                ExpirationMinutes = 30,
                RefreshTokenExpirationDays = 7
            };

            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _sut = new AuthTokenProcessor(
                Options.Create(_jwtOptions),
                _httpContextAccessorMock.Object,
                _userManagerMock.Object);
        }

        [Fact]
        public async Task GenerateToken_ReturnsValidTokenAndExpiry()
        {
            // Arrange
            var user = User.Create("test@test.com", "John", "Doe");
            user.Id = Guid.NewGuid();
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { AppRoles.Visitor });

            // Act
            var (token, expires) = await _sut.GenerateToken(user);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.True(expires > DateTime.UtcNow);
            Assert.True(expires <= DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes + 1));
        }

        [Fact]
        public void GenerateRefreshToken_ReturnsNonEmptyTokenAndFutureExpiry()
        {
            // Act
            var (token, expires) = _sut.GenerateRefreshToken();

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.True(expires > DateTime.UtcNow);
            Assert.True(expires <= DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays + 1));
        }

        [Fact]
        public void GenerateRefreshToken_GeneratesUniqueTokens()
        {
            // Act
            var (token1, _) = _sut.GenerateRefreshToken();
            var (token2, _) = _sut.GenerateRefreshToken();

            // Assert
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void WriteAuthTokenAsHttpOnlyCookie_AppendsCookieToResponse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Act
            _sut.WriteAuthTokenAsHttpOnlyCookie("TEST_COOKIE", "token-value", DateTime.UtcNow.AddHours(1));

            // Assert
            Assert.True(httpContext.Response.Headers.ContainsKey("Set-Cookie"));
            var cookie = httpContext.Response.Headers["Set-Cookie"].ToString();
            Assert.Contains("TEST_COOKIE=token-value", cookie);
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PrepareCTokenClaims_ReturnsValidTokenWithExpectedClaims()
        {
            // Arrange
            var user = User.Create("test@test.com", "John", "Doe");
            user.Id = Guid.NewGuid();
            var roles = new List<string> { AppRoles.Visitor, AppRoles.ClubMember };
            _userManagerMock.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(roles);

            var key = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(_jwtOptions.Secret));

            // Act
            var (token, expires) = await _sut.PrepareCTokenClaims(key, user, SecurityAlgorithms.HmacSha256);

            // Assert
            Assert.NotNull(token);
            Assert.True(expires > DateTime.UtcNow);
            Assert.True(expires <= DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes + 1));

            Assert.Equal(_jwtOptions.Issuer, token.Issuer);
            Assert.Contains(token.Audiences, a => a == _jwtOptions.Audience);

            var claims = token.Claims.ToList();
            Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
            Assert.Contains(claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
            Assert.Contains(claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.ToString());
            Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == AppRoles.Visitor);
            Assert.Contains(claims, c => c.Type == ClaimTypes.Role && c.Value == AppRoles.ClubMember);
        }
    }
}