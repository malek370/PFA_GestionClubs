using GestionClubs.Domain.Pagination;

namespace Domain.Test.Pagination;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        var result = new PagedResult<int>
        {
            TotalCount = 25,
            PageSize = 10
        };

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_ShouldReturnZero_WhenPageSizeIsZero()
    {
        var result = new PagedResult<int>
        {
            TotalCount = 10,
            PageSize = 0
        };

        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void HasPreviousPage_ShouldBeFalse_WhenOnFirstPage()
    {
        var result = new PagedResult<int> { PageNumber = 1 };

        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void HasPreviousPage_ShouldBeTrue_WhenNotOnFirstPage()
    {
        var result = new PagedResult<int> { PageNumber = 2 };

        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void HasNextPage_ShouldBeTrue_WhenNotOnLastPage()
    {
        var result = new PagedResult<int>
        {
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 25
        };

        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void HasNextPage_ShouldBeFalse_WhenOnLastPage()
    {
        var result = new PagedResult<int>
        {
            PageNumber = 3,
            PageSize = 10,
            TotalCount = 25
        };

        Assert.False(result.HasNextPage);
    }
}
