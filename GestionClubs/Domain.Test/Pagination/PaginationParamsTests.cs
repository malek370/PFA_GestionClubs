using GestionClubs.Domain.Pagination;

namespace Domain.Test.Pagination;

public class PaginationParamsTests
{
    [Fact]
    public void PageNumber_ShouldDefaultTo1_WhenNull()
    {
        var p = new PaginationParams { PageNumber = null };

        Assert.Equal(1, p.PageNumber);
    }

    [Fact]
    public void PageNumber_ShouldDefaultTo1_WhenLessThan1()
    {
        var p = new PaginationParams { PageNumber = 0 };

        Assert.Equal(1, p.PageNumber);
    }

    [Fact]
    public void PageSize_ShouldDefaultTo10_WhenNull()
    {
        var p = new PaginationParams { PageSize = null };

        Assert.Equal(10, p.PageSize);
    }

    [Fact]
    public void PageSize_ShouldBeCappedAt100()
    {
        var p = new PaginationParams { PageSize = 200 };

        Assert.Equal(100, p.PageSize);
    }

    [Fact]
    public void PageSize_ShouldDefaultTo10_WhenLessThan1()
    {
        var p = new PaginationParams { PageSize = 0 };

        Assert.Equal(10, p.PageSize);
    }

    [Fact]
    public void PageSize_ShouldReturnValue_WhenInValidRange()
    {
        var p = new PaginationParams { PageSize = 50 };

        Assert.Equal(50, p.PageSize);
    }
}
