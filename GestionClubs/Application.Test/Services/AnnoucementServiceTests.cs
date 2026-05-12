using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Application.Services;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using Moq;
using MockQueryable.Moq;
using MockQueryable;

namespace Application.Test.Services;

public class AnnoucementServiceTests
{
    private readonly Mock<IBaseRepository<Annoucement>> _annoucementRepositoryMock;
    private readonly Mock<IBaseRepository<Club>> _clubRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly AnnoucementService _service;

    public AnnoucementServiceTests()
    {
        _annoucementRepositoryMock = new Mock<IBaseRepository<Annoucement>>();
        _clubRepositoryMock = new Mock<IBaseRepository<Club>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new AnnoucementService(
            _annoucementRepositoryMock.Object,
            _clubRepositoryMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetAnnoucementById_NotFound_ThrowsEntityNotFoundException()
    {
        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns("user@test.com");
        _annoucementRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(new List<Annoucement>().BuildMock());

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetAnnoucementById(1));
    }

    [Fact]
    public async Task GetAnnoucementById_ValidAnnoucement_ReturnsAnnoucement()
    {
        var userEmail = "user@test.com";
        var annoucements = new List<Annoucement>
        {
            new Annoucement
            {
                Id = 1,
                ClubId = 1,
                Title = "Title",
                Content = "Content",
                IsPublic = true,
                Club = new Club { Name = "Test Club", Description = "Desc" }
            }
        }.BuildMock();

        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns(userEmail);
        _annoucementRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(annoucements);

        var result = await _service.GetAnnoucementById(1);

        Assert.NotNull(result);
        Assert.Equal("Title", result.Title);
        Assert.Equal("Test Club", result.ClubName);
    }

    [Fact]
    public async Task CreateAnnoucement_ClubNotFound_ThrowsEntityNotFoundException()
    {
        var createDto = new CreateAnnoucementDTO
        {
            ClubId = 1,
            Title = "Title",
            Content = "Content",
            IsPublic = true
        };

        _clubRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Club)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.CreateAnnoucement(createDto));
    }

    [Fact]
    public async Task CreateAnnoucement_ValidData_ReturnsAnnoucement()
    {
        var createDto = new CreateAnnoucementDTO
        {
            ClubId = 1,
            Title = "Title",
            Content = "Content",
            IsPublic = true
        };

        var club = new Club { Id = 1, Name = "Test Club", Description = "Desc" };

        _clubRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(club);
        _currentUserServiceMock.Setup(x => x.CheckUserIsAdminForClub(It.IsAny<int>())).Returns(Task.CompletedTask);
        _annoucementRepositoryMock.Setup(x => x.Add(It.IsAny<Annoucement>())).ReturnsAsync((Annoucement a) =>
        {
            a.Id = 1;
            return a;
        });

        var result = await _service.CreateAnnoucement(createDto);

        Assert.NotNull(result);
        Assert.Equal("Title", result.Title);
        Assert.Equal("Test Club", result.ClubName);
    }

    [Fact]
    public async Task DeleteAnnoucement_NotFound_ThrowsEntityNotFoundException()
    {
        _annoucementRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Annoucement)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.DeleteAnnoucement(1));
    }

    [Fact]
    public async Task DeleteAnnoucement_ValidAnnoucement_DeletesSuccessfully()
    {
        var annoucement = new Annoucement
        {
            Id = 1,
            ClubId = 1,
            Title = "Title",
            Content = "Content"
        };

        _annoucementRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(annoucement);
        _currentUserServiceMock.Setup(x => x.CheckUserIsAdminForClub(It.IsAny<int>())).Returns(Task.CompletedTask);
        _annoucementRepositoryMock.Setup(x => x.Delete(It.IsAny<int>())).ReturnsAsync(true);

        await _service.DeleteAnnoucement(1);

        _annoucementRepositoryMock.Verify(x => x.Delete(1), Times.Once);
    }
}
