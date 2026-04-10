using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Exceptions;
using IdentityProvider.Requests;
using IdentityProvider.Services;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace IdentityProvider.IdentityProviderTests.Services
{

    public class AccountServiceTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<IAuthTokenProcessor> _authTokenProcessorMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
        private readonly AccountService _sut;

        public AccountServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _passwordHasherMock = new Mock<IPasswordHasher<User>>();

            _userManagerMock = new Mock<UserManager<User>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // Assigner le password hasher directement via la propriété (non mockée)
            _userManagerMock.Object.PasswordHasher = _passwordHasherMock.Object;

            _authTokenProcessorMock = new Mock<IAuthTokenProcessor>();
            _userRepositoryMock = new Mock<IUserRepository>();

            _sut = new AccountService(
                _userManagerMock.Object,
                _authTokenProcessorMock.Object,
                _userRepositoryMock.Object);
        }

        #region LoginAsync

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsLoginFailedException()
        {
            // Arrange
            var request = new LoginRequest { Email = "unknown@test.com", Password = "Pass123!" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<LoginFailedException>(() => _sut.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsLoginFailedException()
        {
            // Arrange
            var user = User.Create("test@test.com", "John", "Doe");
            var request = new LoginRequest { Email = "test@test.com", Password = "WrongPass!" };

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<LoginFailedException>(() => _sut.LoginAsync(request));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_GeneratesTokensAndSetsCookies()
        {
            // Arrange
            var user = User.Create("test@test.com", "John", "Doe");
            var request = new LoginRequest { Email = "test@test.com", Password = "ValidPass1!" };
            var accessToken = "access-token";
            var accessExpiry = DateTime.UtcNow.AddMinutes(30);
            var refreshToken = "refresh-token";
            var refreshExpiry = DateTime.UtcNow.AddDays(7);

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, request.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
            _authTokenProcessorMock.Setup(x => x.GenerateToken(user))
                .ReturnsAsync((accessToken, accessExpiry));
            _authTokenProcessorMock.Setup(x => x.GenerateRefreshToken())
                .Returns((refreshToken, refreshExpiry));

            // Act
            await _sut.LoginAsync(request);

            // Assert
            _authTokenProcessorMock.Verify(x => x.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", accessToken, accessExpiry), Times.Once);
            _authTokenProcessorMock.Verify(x => x.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", refreshToken, refreshExpiry), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(It.Is<User>(u => u.RefreshToken == refreshToken)), Times.Once);
        }

        #endregion

        #region RegisterAsync

        [Fact]
        public async Task RegisterAsync_UserAlreadyExists_ThrowsUserAlreadyExistsException()
        {
            // Arrange
            var existingUser = User.Create("existing@test.com", "Existing", "User");
            var request = new RegisterRequest
            {
                Email = "existing@test.com",
                Password = "Pass123!",
                FirstName = "New",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<User> { existingUser }.AsQueryable());

            // Act & Assert
            await Assert.ThrowsAsync<UserAlreadyExistsException>(() => _sut.RegisterAsync(request));
        }

        [Fact]
        public async Task RegisterAsync_CreateFails_ThrowsRegistrationFailedException()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "new@test.com",
                Password = "Pass123!",
                FirstName = "New",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<User>().AsQueryable());
            _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), request.Password))
                .Returns("hashed-password");
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            // Act & Assert
            await Assert.ThrowsAsync<RegistrationFailedException>(() => _sut.RegisterAsync(request));
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_CreatesUser()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "new@test.com",
                Password = "Pass123!",
                FirstName = "New",
                LastName = "User"
            };

            _userManagerMock.Setup(x => x.Users)
                .Returns(new List<User>().AsQueryable());
            _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), request.Password))
                .Returns("hashed-password");
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _sut.RegisterAsync(request);

            // Assert
            _userManagerMock.Verify(x => x.CreateAsync(It.Is<User>(u =>
                u.Email == request.Email &&
                u.FirstName == request.FirstName &&
                u.LastName == request.LastName &&
                u.UserName == request.Email &&
                u.PasswordHash == "hashed-password"
            )), Times.Once);
        }

        #endregion

        #region RefreshTokenAsync

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task RefreshTokenAsync_NullOrEmptyToken_ThrowsRefreshTokenException(string? token)
        {
            // Act & Assert
            await Assert.ThrowsAsync<RefreshTokenException>(() => _sut.RefreshTokenAsync(token));
        }

        [Fact]
        public async Task RefreshTokenAsync_UserNotFound_ThrowsRefreshTokenException()
        {
            // Arrange
            _userRepositoryMock.Setup(x => x.GetUserByRefreshTokenAsync("invalid-token"))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<RefreshTokenException>(() => _sut.RefreshTokenAsync("invalid-token"));
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ThrowsRefreshTokenException()
        {
            // Arrange
            var user = User.Create("test@test.com", "John", "Doe");
            user.RefreshToken = "expired-token";
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1);

            _userRepositoryMock.Setup(x => x.GetUserByRefreshTokenAsync("expired-token"))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<RefreshTokenException>(() => _sut.RefreshTokenAsync("expired-token"));
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_GeneratesNewTokens()
        {
            // Arrange
            var user = User.Create("test@test.com", "John", "Doe");
            user.RefreshToken = "valid-token";
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(5);

            var newAccessToken = "new-access-token";
            var newAccessExpiry = DateTime.UtcNow.AddMinutes(30);
            var newRefreshToken = "new-refresh-token";
            var newRefreshExpiry = DateTime.UtcNow.AddDays(7);

            _userRepositoryMock.Setup(x => x.GetUserByRefreshTokenAsync("valid-token")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
            _authTokenProcessorMock.Setup(x => x.GenerateToken(user)).ReturnsAsync((newAccessToken, newAccessExpiry));
            _authTokenProcessorMock.Setup(x => x.GenerateRefreshToken()).Returns((newRefreshToken, newRefreshExpiry));

            // Act
            await _sut.RefreshTokenAsync("valid-token");

            // Assert
            _authTokenProcessorMock.Verify(x => x.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", newAccessToken, newAccessExpiry), Times.Once);
            _authTokenProcessorMock.Verify(x => x.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", newRefreshToken, newRefreshExpiry), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(It.Is<User>(u => u.RefreshToken == newRefreshToken)), Times.Once);
        }

        #endregion
    }
}