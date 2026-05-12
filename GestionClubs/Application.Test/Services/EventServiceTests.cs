using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Application.Services;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;
using Moq;

namespace Application.Test.Services;

public class EventServiceTests
{
    private readonly Mock<IBaseRepository<Event>> _eventRepositoryMock;
    private readonly Mock<IBaseRepository<User>> _userRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _eventRepositoryMock = new Mock<IBaseRepository<Event>>();
        _userRepositoryMock = new Mock<IBaseRepository<User>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new EventService(_eventRepositoryMock.Object, _userRepositoryMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task AddEvent_ValidData_ReturnsEvent()
    {
        var createDto = new CreateEventDTO
        {
            ClubId = 1,
            Title = "Test Event",
            Description = "Test Description",
            IsPublic = true,
            Location = "Room A",
            StartDate = DateTime.Now.AddDays(7),
            Tags = new List<string> { "tag1", "tag2" }
        };

        _currentUserServiceMock.Setup(x => x.CheckUserIsAdminForClub(It.IsAny<int>())).Returns(Task.CompletedTask);
        _eventRepositoryMock.Setup(x => x.Add(It.IsAny<Event>())).ReturnsAsync((Event e) =>
        {
            e.Id = 1;
            e.Club = new Club { Name = "Test Club", Description = "Desc" };
            return e;
        });
        _eventRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((int id) =>
            new Event
            {
                Id = id,
                Title = createDto.Title,
                Description = createDto.Description,
                Location = createDto.Location,
                StartDate = createDto.StartDate,
                Club = new Club { Name = "Test Club", Description = "Desc" }
            });

        var result = await _service.AddEvent(createDto);

        Assert.NotNull(result);
        Assert.Equal("Test Event", result.Title);
        Assert.Equal("Test Club", result.ClubName);
    }

    [Fact]
    public async Task DeleteEvent_EventNotFound_ThrowsEntityNotFoundException()
    {
        _eventRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Event)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.DeleteEvent(1));
    }

    [Fact]
    public async Task DeleteEvent_ValidEvent_ReturnsTrue()
    {
        var evt = new Event { Id = 1, ClubId = 1, Title = "Event", Description = "Desc" };

        _eventRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(evt);
        _currentUserServiceMock.Setup(x => x.CheckUserIsAdminForClub(It.IsAny<int>())).Returns(Task.CompletedTask);
        _eventRepositoryMock.Setup(x => x.Delete(It.IsAny<int>())).ReturnsAsync(true);

        var result = await _service.DeleteEvent(1);

        Assert.True(result);
    }

    [Fact]
    public async Task JoinEvent_EventNotFound_ThrowsEntityNotFoundException()
    {
        _eventRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Event)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.JoinEvent(1));
    }

    [Fact]
    public async Task JoinEvent_UserAlreadyJoined_ThrowsInvalidOperationException()
    {
        var userEmail = "user@test.com";
        var evt = new Event
        {
            Id = 1,
            ClubId = 1,
            Title = "Event",
            Description = "Desc",
            Participent = new List<User>
            {
                new User { Email = userEmail, FirstName = "A", LastName = "B" }
            }
        };

        _eventRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(evt);
        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns(userEmail);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.JoinEvent(1));
    }

    [Fact]
    public async Task LeaveEvent_UserNotInEvent_ThrowsInvalidOperationException()
    {
        var userEmail = "user@test.com";
        var evt = new Event
        {
            Id = 1,
            ClubId = 1,
            Title = "Event",
            Description = "Desc",
            Participent = new List<User>()
        };

        _eventRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(evt);
        _currentUserServiceMock.Setup(x => x.GetEmail()).Returns(userEmail);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.LeaveEvent(1));
    }
}
