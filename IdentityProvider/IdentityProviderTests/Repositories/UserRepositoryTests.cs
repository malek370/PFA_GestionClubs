using IdentityProvider.DbContext;
using IdentityProvider.Entities;
using IdentityProvider.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityProvider.Tests.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly IdpDbContext _dbContext;
        private readonly UserRepository _sut;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new IdpDbContext(options);
            _sut = new UserRepository(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public void Constructor_WithValidDbContext_CreatesInstance()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new IdpDbContext(options);

            // Act
            var repository = new UserRepository(dbContext);

            // Assert
            Assert.NotNull(repository);
        }

        [Fact]
        public async Task GetUserByRefreshTokenAsync_UserWithMatchingToken_ReturnsUser()
        {
            // Arrange
            var refreshToken = "test-refresh-token-123";
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "test@example.com",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _sut.GetUserByRefreshTokenAsync(refreshToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(refreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task GetUserByRefreshTokenAsync_NoMatchingToken_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "test@example.com",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                RefreshToken = "different-token",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _sut.GetUserByRefreshTokenAsync("non-existent-token");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByRefreshTokenAsync_EmptyDatabase_ReturnsNull()
        {
            // Arrange
            // Database is empty (no users added)

            // Act
            var result = await _sut.GetUserByRefreshTokenAsync("any-token");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByRefreshTokenAsync_MultipleUsersOnlyOneMatches_ReturnsMatchingUser()
        {
            // Arrange
            var targetToken = "target-token";
            var user1 = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user1@example.com",
                Email = "user1@example.com",
                FirstName = "User",
                LastName = "One",
                RefreshToken = "token-1",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user2@example.com",
                Email = "user2@example.com",
                FirstName = "User",
                LastName = "Two",
                RefreshToken = targetToken,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            var user3 = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user3@example.com",
                Email = "user3@example.com",
                FirstName = "User",
                LastName = "Three",
                RefreshToken = "token-3",
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _dbContext.Users.AddRange(user1, user2, user3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _sut.GetUserByRefreshTokenAsync(targetToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user2.Id, result.Id);
            Assert.Equal(targetToken, result.RefreshToken);
        }

        [Fact]
        public async Task GetUserByRefreshTokenAsync_UserWithNullRefreshToken_ReturnsNull()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "test@example.com",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                RefreshToken = null,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _sut.GetUserByRefreshTokenAsync("some-token");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetUserByRefreshTokenAsync_EmptyStringToken_ReturnsUserWithEmptyToken()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = "test@example.com",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                RefreshToken = string.Empty,
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _sut.GetUserByRefreshTokenAsync(string.Empty);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            Assert.Equal(string.Empty, result.RefreshToken);
        }
    }
}
