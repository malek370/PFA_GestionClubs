using IdentityProvider.Entities;
using IdentityProvider.Processors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Xunit;
using JwtOptions = IdentityProvider.Options.JwtOptions;

namespace IdentityProvider.Tests.Processors;

public class AuthTokenProcessorAssymetricKeyTests : IDisposable
{
    private readonly JwtOptions _jwtOptions;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly AuthTokenProcessorAssymetricKey _sut;
    private const string TestPrivateKeyPath = "private_key.pem";
    private readonly string _privateKeyPem;

    public AuthTokenProcessorAssymetricKeyTests()
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

        // Generate a test RSA private key in PEM format
        using var rsa = RSA.Create(2048);
        _privateKeyPem = rsa.ExportRSAPrivateKeyPem();

        // Write the private key to file for the method to read
        File.WriteAllText(TestPrivateKeyPath, _privateKeyPem);

        _sut = new AuthTokenProcessorAssymetricKey(
            Microsoft.Extensions.Options.Options.Create(_jwtOptions),
            _httpContextAccessorMock.Object,
            _userManagerMock.Object);
    }

    [Fact]
    public async Task GenerateToken_ValidUser_ReturnsTokenAndExpiry()
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
    public async Task GenerateToken_ValidUser_CreatesJwtTokenWithRsaSignature()
    {
        // Arrange
        var user = User.Create("test@test.com", "Jane", "Smith");
        user.Id = Guid.NewGuid();
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { AppRoles.ClubAdmin });

        // Act
        var (token, expires) = await _sut.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.NotNull(jwtToken);
        Assert.Equal("RS256", jwtToken.Header.Alg);
        Assert.Equal("1", jwtToken.Header.Kid);
    }

    [Fact]
    public async Task GenerateToken_UserWithMultipleRoles_IncludesAllRolesInToken()
    {
        // Arrange
        var user = User.Create("admin@test.com", "Admin", "User");
        user.Id = Guid.NewGuid();
        var roles = new List<string> { AppRoles.Visitor, AppRoles.ClubMember, AppRoles.ClubAdmin };
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var (token, expires) = await _sut.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var roleClaims = jwtToken.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").ToList();
        Assert.Equal(roles.Count, roleClaims.Count);
    }

    [Fact]
    public async Task GenerateToken_ValidUser_TokenContainsUserIdentifierClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create("user@example.com", "First", "Last");
        user.Id = userId;
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var (token, _) = await _sut.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Contains(jwtToken.Claims, c => c.Type == "sub" && c.Value == userId.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == "email" && c.Value == "user@example.com");
    }

    [Fact]
    public async Task GenerateToken_ValidUser_TokenContainsIssuerAndAudience()
    {
        // Arrange
        var user = User.Create("test@test.com", "Test", "User");
        user.Id = Guid.NewGuid();
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var (token, _) = await _sut.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal(_jwtOptions.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtOptions.Audience, jwtToken.Audiences);
    }

    [Fact]
    public async Task GenerateToken_CalledMultipleTimes_GeneratesUniqueTokens()
    {
        // Arrange
        var user = User.Create("test@test.com", "Test", "User");
        user.Id = Guid.NewGuid();
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var (token1, _) = await _sut.GenerateToken(user);
        var (token2, _) = await _sut.GenerateToken(user);

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public async Task GenerateToken_ValidUser_ExpiresMatchesJwtOptions()
    {
        // Arrange
        var user = User.Create("test@test.com", "Test", "User");
        user.Id = Guid.NewGuid();
        _userManagerMock.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());
        var beforeCall = DateTime.UtcNow;

        // Act
        var (_, expires) = await _sut.GenerateToken(user);
        var afterCall = DateTime.UtcNow;

        // Assert
        var expectedMinExpiry = beforeCall.AddMinutes(_jwtOptions.ExpirationMinutes);
        var expectedMaxExpiry = afterCall.AddMinutes(_jwtOptions.ExpirationMinutes);
        Assert.True(expires >= expectedMinExpiry && expires <= expectedMaxExpiry);
    }


    public void Dispose()
    {
        // Clean up the test private key file
        if (File.Exists(TestPrivateKeyPath))
        {
            File.Delete(TestPrivateKeyPath);
        }
    }
}
