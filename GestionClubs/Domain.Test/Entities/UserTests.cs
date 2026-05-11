using GestionClubs.Domain.Entities;

namespace Domain.Test.Entities;

public class UserTests
{
    [Fact]
    public void User_ShouldInheritFromBaseEntity()
    {
        var user = new User { Email = "test@test.com", FirstName = "John", LastName = "Doe" };

        Assert.IsAssignableFrom<BaseEntity>(user);
    }

    [Fact]
    public void User_ShouldSetProperties()
    {
        var user = new User { Email = "user@example.com", FirstName = "Jane", LastName = "Smith" };

        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Smith", user.LastName);
    }

    [Fact]
    public void User_Events_ShouldBeInitializedEmpty()
    {
        var user = new User { Email = "test@test.com", FirstName = "A", LastName = "B" };

        Assert.Empty(user.Events);
    }
}
