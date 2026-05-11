using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;

namespace Domain.Test.Entities;

public class MemberTests
{
    [Fact]
    public void Member_ShouldInheritFromBaseEntity()
    {
        var member = new Member { PostInClub = ClubPost.Member };

        Assert.IsAssignableFrom<BaseEntity>(member);
    }

    [Fact]
    public void Member_ShouldSetProperties()
    {
        var member = new Member
        {
            ClubId = 1,
            UserId = 2,
            PostInClub = ClubPost.President
        };

        Assert.Equal(1, member.ClubId);
        Assert.Equal(2, member.UserId);
        Assert.Equal(ClubPost.President, member.PostInClub);
    }

    [Fact]
    public void Member_NavigationProperties_ShouldBeNullByDefault()
    {
        var member = new Member { PostInClub = ClubPost.Member };

        Assert.Null(member.Club);
        Assert.Null(member.User);
    }
}
