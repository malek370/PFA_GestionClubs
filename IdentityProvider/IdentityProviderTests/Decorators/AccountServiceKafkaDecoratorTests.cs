using IdentityProvider.Abstracts;
using IdentityProvider.Decorators;
using IdentityProvider.Entities;
using IdentityProvider.Events;
using IdentityProvider.Kafka;
using IdentityProvider.Options;
using IdentityProvider.Requests;
using IdentityProvider.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IdentityProvider.Tests.Decorators
{
    public class AccountServiceKafkaDecoratorTests
    {
        private readonly Mock<IAccountService> _innerServiceMock;
        private readonly Mock<IKafkaProducer> _producerMock;
        private readonly Mock<IOptions<KafkaOptions>> _optionsMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly AccountServiceKafkaDecorator _sut;
        private readonly KafkaOptions _kafkaOptions;

        public AccountServiceKafkaDecoratorTests()
        {
            _innerServiceMock = new Mock<IAccountService>();
            _producerMock = new Mock<IKafkaProducer>();
            _optionsMock = new Mock<IOptions<KafkaOptions>>();
            
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _kafkaOptions = new KafkaOptions
            {
                ProducerTopic = "user-registered"
            };
            _optionsMock.Setup(x => x.Value).Returns(_kafkaOptions);

            _sut = new AccountServiceKafkaDecorator(
                _innerServiceMock.Object,
                _producerMock.Object,
                _optionsMock.Object,
                _userManagerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_CallsInnerService_Successfully()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Password = "Pass123!",
                ConfirmPassword = "Pass123!",
                FirstName = "John",
                LastName = "Doe"
            };

            _innerServiceMock.Setup(x => x.RegisterAsync(request))
                .Returns(Task.CompletedTask);

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // Act
            await _sut.RegisterAsync(request);

            // Assert
            _innerServiceMock.Verify(x => x.RegisterAsync(request), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_UserFound_PublishesUserRegisteredEvent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Password = "Pass123!",
                ConfirmPassword = "Pass123!",
                FirstName = "John",
                LastName = "Doe"
            };

            var user = new User
            {
                Id = userId,
                Email = request.Email,
                UserName = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            _innerServiceMock.Setup(x => x.RegisterAsync(request))
                .Returns(Task.CompletedTask);

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _producerMock.Setup(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserRegisteredEvent>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _sut.RegisterAsync(request);

            // Assert
            _producerMock.Verify(x => x.PublishAsync(
                _kafkaOptions.ProducerTopic,
                userId.ToString(),
                It.Is<UserRegisteredEvent>(e =>
                    e.UserId == userId &&
                    e.Email == request.Email &&
                    e.FirstName == request.FirstName &&
                    e.LastName == request.LastName),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_UserNotFound_DoesNotPublishEvent()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@example.com",
                Password = "Pass123!",
                ConfirmPassword = "Pass123!",
                FirstName = "John",
                LastName = "Doe"
            };

            _innerServiceMock.Setup(x => x.RegisterAsync(request))
                .Returns(Task.CompletedTask);

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // Act
            await _sut.RegisterAsync(request);

            // Assert
            _producerMock.Verify(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<UserRegisteredEvent>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task LoginAsync_DelegatesToInnerService_ReturnsTokenResponse()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Pass123!"
            };

            var expectedResponse = new TokenResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            };

            _innerServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.Equal(expectedResponse, result);
            _innerServiceMock.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>()), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_DelegatesToInnerService_ReturnsTokenResponse()
        {
            // Arrange
            var refreshToken = "refresh-token-123";
            var expectedResponse = new TokenResponse
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token"
            };

            _innerServiceMock.Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _sut.RefreshTokenAsync(refreshToken);

            // Assert
            Assert.Equal(expectedResponse, result);
            _innerServiceMock.Verify(x => x.RefreshTokenAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_NullToken_DelegatesToInnerService()
        {
            // Arrange
            string? refreshToken = null;
            var expectedResponse = new TokenResponse
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            };

            _innerServiceMock.Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _sut.RefreshTokenAsync(refreshToken);

            // Assert
            Assert.Equal(expectedResponse, result);
            _innerServiceMock.Verify(x => x.RefreshTokenAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Constructor_InitializesAllDependencies_VerifiedThroughBehavior()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "constructor-test@example.com",
                Password = "Pass123!",
                ConfirmPassword = "Pass123!",
                FirstName = "Test",
                LastName = "User"
            };

            _innerServiceMock.Setup(x => x.RegisterAsync(request))
                .Returns(Task.CompletedTask);

            _userManagerMock.Setup(x => x.FindByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // Act
            await _sut.RegisterAsync(request);

            // Assert - Verifies all dependencies are properly initialized
            _innerServiceMock.Verify(x => x.RegisterAsync(request), Times.Once);
            _userManagerMock.Verify(x => x.FindByEmailAsync(request.Email), Times.Once);
        }
    }
}
