using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;

namespace Domain.Test.Entities;

public class AdhesionTests
{
    [Fact]
    public void Adhesion_ShouldInheritFromBaseEntity()
    {
        var adhesion = new Adhesion { ClubId = 1 };

        Assert.IsAssignableFrom<BaseEntity>(adhesion);
    }

    [Fact]
    public void Adhesion_ShouldSetProperties()
    {
        var adhesion = new Adhesion
        {
            ClubId = 1,
            UserId = 2,
            Status = Status.Accepted
        };

        Assert.Equal(1, adhesion.ClubId);
        Assert.Equal(2, adhesion.UserId);
        Assert.Equal(Status.Accepted, adhesion.Status);
    }

    [Fact]
    public void Adhesion_DefaultStatus_ShouldBePending()
    {
        var adhesion = new Adhesion { ClubId = 1 };

        Assert.Equal(Status.Pending, adhesion.Status);
    }
}
