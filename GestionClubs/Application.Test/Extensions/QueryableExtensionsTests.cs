using GestionClubs.Domain.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Application.Test.Extensions;

public class QueryableExtensionsTests
{
    private DbContextOptions<TestDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task ToPagedResultAsync_ReturnsCorrectPage()
    {
        var options = CreateInMemoryOptions();
        using var context = new TestDbContext(options);

        for (int i = 1; i <= 25; i++)
        {
            context.TestItems.Add(new TestItem { Id = i, Name = $"Item {i}" });
        }
        await context.SaveChangesAsync();

        var pagination = new PaginationParams { PageNumber = 2, PageSize = 10 };
        var query = context.TestItems.AsQueryable();

        var result = await GestionClubs.Application.Extensions.QueryableExtensions.ToPagedResultAsync(query, pagination);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task ToPagedResultAsync_FirstPage_HasNoPreviousPage()
    {
        var options = CreateInMemoryOptions();
        using var context = new TestDbContext(options);

        for (int i = 1; i <= 10; i++)
        {
            context.TestItems.Add(new TestItem { Id = i, Name = $"Item {i}" });
        }
        await context.SaveChangesAsync();

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 5 };
        var query = context.TestItems.AsQueryable();

        var result = await GestionClubs.Application.Extensions.QueryableExtensions.ToPagedResultAsync(query, pagination);

        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public async Task ToPagedResultAsync_LastPage_HasNoNextPage()
    {
        var options = CreateInMemoryOptions();
        using var context = new TestDbContext(options);

        for (int i = 1; i <= 10; i++)
        {
            context.TestItems.Add(new TestItem { Id = i, Name = $"Item {i}" });
        }
        await context.SaveChangesAsync();

        var pagination = new PaginationParams { PageNumber = 2, PageSize = 5 };
        var query = context.TestItems.AsQueryable();

        var result = await GestionClubs.Application.Extensions.QueryableExtensions.ToPagedResultAsync(query, pagination);

        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task ToPagedResultAsync_NullPagination_UsesDefaults()
    {
        var options = CreateInMemoryOptions();
        using var context = new TestDbContext(options);

        for (int i = 1; i <= 15; i++)
        {
            context.TestItems.Add(new TestItem { Id = i, Name = $"Item {i}" });
        }
        await context.SaveChangesAsync();

        var query = context.TestItems.AsQueryable();

        var result = await GestionClubs.Application.Extensions.QueryableExtensions.ToPagedResultAsync(query, null);

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(15, result.TotalCount);
    }

    public class TestItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<TestItem> TestItems { get; set; }
    }
}
