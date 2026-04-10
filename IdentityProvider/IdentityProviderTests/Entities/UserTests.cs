using IdentityProvider.Entities;
using Xunit;

namespace IdentityProvider.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_SetsPropertiesCorrectly()
    {
        // Act
        var user = User.Create("test@test.com", "John", "Doe");

        // Assert
        Assert.Equal("test@test.com", user.Email);
        Assert.Equal("test@test.com", user.UserName);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var user = User.Create("test@test.com", "John", "Doe");

        // Act
        var result = user.ToString();

        // Assert
        Assert.Equal("John Doe (test@test.com)", result);
    }
}