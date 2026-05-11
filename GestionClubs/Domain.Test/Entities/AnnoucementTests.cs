using GestionClubs.Domain.Entities;

namespace Domain.Test.Entities;

public class AnnoucementTests
{
    [Fact]
    public void Annoucement_ShouldInheritFromBaseEntity()
    {
        var annoucement = new Annoucement { ClubId = 1, Title = "T", Content = "C" };

        Assert.IsAssignableFrom<BaseEntity>(annoucement);
    }

    [Fact]
    public void Annoucement_ShouldSetProperties()
    {
        var annoucement = new Annoucement
        {
            ClubId = 1,
            Title = "Important",
            Content = "Meeting tomorrow",
            IsPublic = true
        };

        Assert.Equal(1, annoucement.ClubId);
        Assert.Equal("Important", annoucement.Title);
        Assert.Equal("Meeting tomorrow", annoucement.Content);
        Assert.True(annoucement.IsPublic);
    }

    [Fact]
    public void Annoucement_IsPublic_ShouldDefaultToFalse()
    {
        var annoucement = new Annoucement { ClubId = 1, Title = "T", Content = "C" };

        Assert.False(annoucement.IsPublic);
    }
}
