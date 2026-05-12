using GestionClubs.Domain.Entities;

namespace Domain.Test.Entities;

public class EventTests
{
    [Fact]
    public void Event_ShouldInheritFromBaseEntity()
    {
        var evt = new Event { Title = "Event1", Description = "Desc" };

        Assert.IsAssignableFrom<BaseEntity>(evt);
    }

    [Fact]
    public void Event_ShouldSetProperties()
    {
        var evt = new Event
        {
            Title = "Hackathon",
            Description = "Annual hackathon",
            IsPublic = true,
            Location = "Room A",
            ClubId = 1,
            StartDate = new DateTime(2025, 6, 15)
        };

        Assert.Equal("Hackathon", evt.Title);
        Assert.Equal("Annual hackathon", evt.Description);
        Assert.True(evt.IsPublic);
        Assert.Equal("Room A", evt.Location);
        Assert.Equal(1, evt.ClubId);
        Assert.Equal(new DateTime(2025, 6, 15), evt.StartDate);
    }

    [Fact]
    public void Event_Collections_ShouldBeInitializedEmpty()
    {
        var evt = new Event { Title = "T", Description = "D" };

        Assert.Empty(evt.Tags);
        Assert.Empty(evt.Participent);
    }
}
