using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.Exceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Application.Services;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using Moq;
using MockQueryable.Moq;
using MockQueryable;

namespace Application.Test.Services;

public class AdhesionServiceTests
{
    private readonly Mock<IBaseRepository<Adhesion>> _adhesionRepositoryMock;
    private readonly Mock<IBaseRepository<Member>> _memberRepositoryMock;
    private readonly Mock<IBaseRepository<User>> _userRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly AdhesionService _service;

    public AdhesionServiceTests()
    {
        _adhesionRepositoryMock = new Mock<IBaseRepository<Adhesion>>();
        _memberRepositoryMock = new Mock<IBaseRepository<Member>>();
        _userRepositoryMock = new Mock<IBaseRepository<User>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new AdhesionService(
            _adhesionRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _userRepositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task AddAdhesion_UserAlreadyRequested_ThrowsUserAdhesionExistsException()
    {
        var userEmail = "user@test.com";
        var createDto = new CreateAdhesionDTO { ClubId = 1 };

        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns(userEmail);

        var adhesions = new List<Adhesion>
        {
            new Adhesion
            {
                ClubId = 1,
                User = new User { Email = userEmail, FirstName = "A", LastName = "B" }
            }
        }.BuildMock();

        _adhesionRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(adhesions);

        await Assert.ThrowsAsync<UserAdhesionExistsException>(() => _service.AddAdhesion(createDto));
    }

    [Fact]
    public async Task AddAdhesion_UserAlreadyMember_ThrowsUserAlreadyMemberException()
    {
        var userEmail = "user@test.com";
        var createDto = new CreateAdhesionDTO { ClubId = 1 };

        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns(userEmail);

        var adhesions = new List<Adhesion>().BuildMock();
        var members = new List<Member>
        {
            new Member
            {
                ClubId = 1,
                User = new User { Email = userEmail, FirstName = "A", LastName = "B" },
                PostInClub = ClubPost.Member
            }
        }.BuildMock();

        _adhesionRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(adhesions);
        _memberRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(members);

        await Assert.ThrowsAsync<UserAlreadyMemberException>(() => _service.AddAdhesion(createDto));
    }

    [Fact]
    public async Task AddAdhesion_ValidRequest_ReturnsAdhesion()
    {
        var userEmail = "user@test.com";
        var createDto = new CreateAdhesionDTO { ClubId = 1 };
        var user = new User { Id = 1, Email = userEmail, FirstName = "John", LastName = "Doe" };

        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns(userEmail);
        _adhesionRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(new List<Adhesion>().BuildMock());
        _memberRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(new List<Member>().BuildMock());
        _userRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(new List<User> { user }.BuildMock());
        _adhesionRepositoryMock.Setup(x => x.Add(It.IsAny<Adhesion>())).ReturnsAsync((Adhesion a) =>
        {
            a.Id = 1;
            a.Club = new Club { Name = "Test Club", Description = "Desc" };
            return a;
        });

        var result = await _service.AddAdhesion(createDto);

        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("Test Club", result.ClubName);
    }

    [Fact]
    public async Task AcceptAdhesion_AdhesionNotFound_ThrowsEntityNotFoundException()
    {
        _adhesionRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Adhesion)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.AcceptAdhesion(1));
    }

    [Fact]
    public async Task AcceptAdhesion_ValidAdhesion_ReturnsAcceptedAdhesion()
    {
        var adhesion = new Adhesion
        {
            Id = 1,
            ClubId = 1,
            UserId = 1,
            Status = Status.Pending,
            Club = new Club { Name = "Test Club", Description = "Desc" },
            User = new User { Id = 1, Email = "user@test.com", FirstName = "John", LastName = "Doe" }
        };

        _adhesionRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(adhesion);
        _currentUserServiceMock.Setup(x => x.CheckUserIsAdminForClub(It.IsAny<int>())).Returns(Task.CompletedTask);
        _memberRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(new List<Member>().BuildMock());
        _userRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(new List<User> { adhesion.User }.BuildMock());
        _memberRepositoryMock.Setup(x => x.Add(It.IsAny<Member>())).ReturnsAsync(new Member { PostInClub = ClubPost.Member });
        _adhesionRepositoryMock.Setup(x => x.Update(It.IsAny<Adhesion>())).ReturnsAsync((Adhesion a) => a);

        var result = await _service.AcceptAdhesion(1);

        Assert.NotNull(result);
        Assert.Equal("Accepted", result.Status);
    }

    [Fact]
    public async Task RefuseAdhesion_AdhesionNotFound_ThrowsEntityNotFoundException()
    {
        _adhesionRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Adhesion)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.RefuseAdhesion(1));
    }

    [Fact]
    public async Task DeleteAdhesion_AdhesionNotFound_ThrowsEntityNotFoundException()
    {
        _adhesionRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(new List<Adhesion>().BuildMock());

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.DeleteAdhesion(1));
    }

    [Fact]
    public async Task DeleteAdhesion_ValidAdhesion_ReturnsTrue()
    {
        var adhesions = new List<Adhesion>
        {
            new Adhesion { Id = 1, ClubId = 1 }
        }.BuildMock();

        _adhesionRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(adhesions);
        _adhesionRepositoryMock.Setup(x => x.Delete(It.IsAny<int>())).ReturnsAsync(true);

        var result = await _service.DeleteAdhesion(1);

        Assert.True(result);
    }
}
