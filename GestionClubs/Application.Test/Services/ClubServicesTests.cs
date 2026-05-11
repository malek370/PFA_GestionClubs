using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.Exceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Application.Services;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using GestionClubs.Domain.Pagination;
using Moq;
using MockQueryable.Moq;
using MockQueryable;

namespace Application.Test.Services;

public class ClubServicesTests
{
    private readonly Mock<IBaseRepository<Club>> _clubRepositoryMock;
    private readonly Mock<IBaseRepository<User>> _userRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly ClubServices _service;

    public ClubServicesTests()
    {
        _clubRepositoryMock = new Mock<IBaseRepository<Club>>();
        _userRepositoryMock = new Mock<IBaseRepository<User>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new ClubServices(_clubRepositoryMock.Object, _userRepositoryMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateClub_ValidData_ReturnsClub()
    {
        var createDto = new CreateClubDTO
        {
            Name = "Test Club",
            Description = "Test Description",
            Email = "president@test.com",
            Documents = new System.Collections.ObjectModel.Collection<string>()
        };

        var user = new User { Id = 1, Email = "president@test.com", FirstName = "John", LastName = "Doe" };
        var clubs = new List<Club>().BuildMock();

        _clubRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(clubs);
        _userRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(new List<User> { user }.BuildMock());
        _clubRepositoryMock.Setup(x => x.Add(It.IsAny<Club>())).ReturnsAsync((Club c) =>
        {
            c.Id = 1;
            return c;
        });

        var result = await _service.CreateClub(createDto);

        Assert.NotNull(result);
        Assert.Equal("Test Club", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal("president@test.com", result.PresidentMail);
    }

    [Fact]
    public async Task CreateClub_ClubAlreadyExists_ThrowsClubExistsException()
    {
        var createDto = new CreateClubDTO
        {
            Name = "Existing Club",
            Description = "Desc",
            Email = "test@test.com",
            Documents = new System.Collections.ObjectModel.Collection<string>()
        };

        var clubs = new List<Club>
        {
            new Club { Name = "EXISTING CLUB", Description = "Desc" }
        }.BuildMock();

        _clubRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(clubs);

        await Assert.ThrowsAsync<ClubExistsException>(() => _service.CreateClub(createDto));
    }

    [Fact]
    public async Task CreateClub_UserNotFound_ThrowsEntityNotFoundException()
    {
        var createDto = new CreateClubDTO
        {
            Name = "Test Club",
            Description = "Desc",
            Email = "nonexistent@test.com",
            Documents = new System.Collections.ObjectModel.Collection<string>()
        };

        var clubs = new List<Club>().BuildMock();
        var users = new List<User>().BuildMock();

        _clubRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(clubs);
        _userRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(users);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.CreateClub(createDto));
    }

    [Fact]
    public async Task GetClubs_WithFilters_ReturnsFilteredClubs()
    {
        var clubs = new List<Club>
        {
            new Club
            {
                Id = 1,
                Name = "Test Club",
                Description = "Test Description",
                Members = new System.Collections.ObjectModel.Collection<Member>
                {
                    new Member
                    {
                        PostInClub = ClubPost.President,
                        User = new User { Email = "pres@test.com", FirstName = "A", LastName = "B" }
                    }
                }
            },
            new Club
            {
                Id = 2,
                Name = "Another Club",
                Description = "Another Description",
                Members = new System.Collections.ObjectModel.Collection<Member>
                {
                    new Member
                    {
                        PostInClub = ClubPost.President,
                        User = new User { Email = "pres2@test.com", FirstName = "C", LastName = "D" }
                    }
                }
            }
        }.BuildMock();

        _clubRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(clubs);

        var filter = new FilterClubDTO { Name = "Test" };
        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };

        var result = await _service.GetClubs(filter, pagination);

        Assert.Equal(1, result.Items.Count);
        Assert.Equal("Test Club", result.Items[0].Name);
    }

    [Fact]
    public async Task GetUserClubs_ReturnsUserClubs()
    {
        var userEmail = "user@test.com";
        var clubs = new List<Club>
        {
            new Club
            {
                Id = 1,
                Name = "User Club",
                Description = "User Description",
                Members = new System.Collections.ObjectModel.Collection<Member>
                {
                    new Member
                    {
                        PostInClub = ClubPost.President,
                        User = new User { Email = "pres@test.com", FirstName = "A", LastName = "B" }
                    },
                    new Member
                    {
                        PostInClub = ClubPost.Member,
                        User = new User { Email = userEmail, FirstName = "C", LastName = "D" }
                    }
                }
            }
        }.BuildMock();

        _clubRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(clubs);
        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns(userEmail);

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };

        var result = await _service.GetUserClubs(pagination);

        Assert.Equal(1, result.Items.Count);
        Assert.Equal("User Club", result.Items[0].Name);
        Assert.Equal("Member", result.Items[0].UserPost);
    }

    [Fact]
    public async Task GetUserClubs_UserNotAuthenticated_ThrowsUnauthorizedAccessException()
    {
        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns((string)null);

        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetUserClubs(pagination));
    }
}
