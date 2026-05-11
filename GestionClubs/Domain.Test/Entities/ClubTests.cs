using GestionClubs.Domain.Entities;

namespace Domain.Test.Entities;

public class ClubTests
{
    [Fact]
    public void Club_ShouldInheritFromBaseEntity()
    {
        var club = new Club { Name = "Test Club", Description = "Description" };

        Assert.IsAssignableFrom<BaseEntity>(club);
    }

    [Fact]
    public void Club_ShouldInitializeCollectionsEmpty()
    {
        var club = new Club { Name = "Test Club", Description = "Description" };

        Assert.Empty(club.Documents);
        Assert.Empty(club.Members);
        Assert.Empty(club.Adhesions);
        Assert.Empty(club.Annoucements);
    }

    [Fact]
    public void Club_ShouldSetProperties()
    {
        var club = new Club { Name = "Club Dev", Description = "A dev club" };

        Assert.Equal("Club Dev", club.Name);
        Assert.Equal("A dev club", club.Description);
    }

    [Fact]
    public void Club_CreationDate_ShouldBeSetAutomatically()
    {
        var before = DateTime.UtcNow;
        var club = new Club { Name = "Test", Description = "Desc" };
        var after = DateTime.UtcNow;

        Assert.InRange(club.CreatinDate, before, after);
    }
}
