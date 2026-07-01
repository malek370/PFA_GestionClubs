using IdentityProvider.DbContext;
using IdentityProvider.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace IdentityProvider.Tests.DbContext
{
    public class IdpDbContextTests
    {
        [Fact]
        public void OnModelCreating_ConfiguresUserEntity_WithCorrectTableName()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_TableName")
                .Options;

            // Act
            using var context = new IdpDbContext(options);
            var entityType = context.Model.FindEntityType(typeof(User));

            // Assert
            Assert.NotNull(entityType);
            Assert.Equal("Users", entityType.GetTableName());
        }

        [Fact]
        public void OnModelCreating_ConfiguresUserEntity_WithPrimaryKey()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_PrimaryKey")
                .Options;

            // Act
            using var context = new IdpDbContext(options);
            var entityType = context.Model.FindEntityType(typeof(User));

            // Assert
            Assert.NotNull(entityType);
            var primaryKey = entityType.FindPrimaryKey();
            Assert.NotNull(primaryKey);
            Assert.Single(primaryKey.Properties);
            Assert.Equal("Id", primaryKey.Properties[0].Name);
        }

        [Fact]
        public void OnModelCreating_ConfiguresUserNameProperty_AsRequired()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_UserNameRequired")
                .Options;

            // Act
            using var context = new IdpDbContext(options);
            var entityType = context.Model.FindEntityType(typeof(User));

            // Assert
            Assert.NotNull(entityType);
            var userNameProperty = entityType.FindProperty("UserName");
            Assert.NotNull(userNameProperty);
            Assert.False(userNameProperty.IsNullable);
        }

        [Fact]
        public void OnModelCreating_ConfiguresUserNameProperty_WithMaxLength256()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_UserNameMaxLength")
                .Options;

            // Act
            using var context = new IdpDbContext(options);
            var entityType = context.Model.FindEntityType(typeof(User));

            // Assert
            Assert.NotNull(entityType);
            var userNameProperty = entityType.FindProperty("UserName");
            Assert.NotNull(userNameProperty);
            Assert.Equal(256, userNameProperty.GetMaxLength());
        }

        [Fact]
        public void OnModelCreating_ConfiguresEmailProperty_AsRequired()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_EmailRequired")
                .Options;

            // Act
            using var context = new IdpDbContext(options);
            var entityType = context.Model.FindEntityType(typeof(User));

            // Assert
            Assert.NotNull(entityType);
            var emailProperty = entityType.FindProperty("Email");
            Assert.NotNull(emailProperty);
            Assert.False(emailProperty.IsNullable);
        }

        [Fact]
        public void OnModelCreating_ConfiguresEmailProperty_WithMaxLength256()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_EmailMaxLength")
                .Options;

            // Act
            using var context = new IdpDbContext(options);
            var entityType = context.Model.FindEntityType(typeof(User));

            // Assert
            Assert.NotNull(entityType);
            var emailProperty = entityType.FindProperty("Email");
            Assert.NotNull(emailProperty);
            Assert.Equal(256, emailProperty.GetMaxLength());
        }

        [Fact]
        public void OnModelCreating_CallsBaseOnModelCreating_ConfiguresIdentityTables()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<IdpDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_BaseConfiguration")
                .Options;

            // Act
            using var context = new IdpDbContext(options);
            var model = context.Model;

            // Assert - Verify that base Identity tables are configured
            var userRoleType = model.FindEntityType("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>");
            Assert.NotNull(userRoleType);
        }
    }
}
