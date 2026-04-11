using IdentityProvider.Abstracts;
using IdentityProvider.Exceptions;
using IdentityProvider.Requests;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IdentityProvider.IdentityProviderTests.Endpoints;

public class EndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<IAccountService> _accountServiceMock;

    public EndpointTests(CustomWebApplicationFactory factory)
    {
        _accountServiceMock = factory.AccountServiceMock;
        _client = factory.CreateClient();
    }

    #region Register

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@test.com",
            Password = "Pass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _accountServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _accountServiceMock.Verify(x => x.RegisterAsync(It.Is<RegisterRequest>(r =>
            r.Email == request.Email)), Times.Once);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "exists@test.com",
            Password = "Pass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _accountServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ThrowsAsync(new UserAlreadyExistsException(request.Email));

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    #endregion

    #region Login

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest { Email = "test@test.com", Password = "Pass123!" };

        _accountServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest { Email = "bad@test.com", Password = "Wrong!" };

        _accountServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new LoginFailedException(request.Email));

        // Act
        var response = await _client.PostAsJsonAsync("/api/account/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region RefreshToken

    [Fact]
    public async Task RefreshToken_ValidCookie_ReturnsOk()
    {
        // Arrange
        _accountServiceMock
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/account/refresh-token");
        requestMessage.Headers.Add("Cookie", "REFRESH_TOKEN=valid-token");

        // Act
        var response = await _client.SendAsync(requestMessage);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_MissingCookie_ReturnsUnauthorized()
    {
        // Arrange
        _accountServiceMock
            .Setup(x => x.RefreshTokenAsync(null))
            .ThrowsAsync(new RefreshTokenException("Refresh token is missing"));

        // Act
        var response = await _client.PostAsync("/api/account/refresh-token", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Protected Endpoints

    [Fact]
    public async Task ProtectedEndpoint_NoAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/account/protected");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MemberEndpoint_NoAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/account/member");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlatformAdminEndpoint_NoAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/account/platformadmin");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}