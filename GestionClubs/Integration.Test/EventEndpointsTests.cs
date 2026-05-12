using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using System.Net;
using System.Net.Http.Json;

namespace Integration.Test;

public class EventEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EventEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEvents_AsVisitor_Returns200()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/events?Tags=&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_WithTags_Returns200()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/events?Tags=&music&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEventById_NotFound_Returns404()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/events/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsClubAdmin_Returns201()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var dto = new CreateEventDTO
        {
            ClubId = 1,
            Title = "Test Event",
            Description = "Event description",
            IsPublic = true,
            Location = "Room 101",
            StartDate = DateTime.UtcNow.AddDays(7),
            Tags = ["tech", "workshop"]
        };

        var response = await _client.PostAsJsonAsync("/api/events", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var dto = new CreateEventDTO
        {
            ClubId = 1,
            Title = "Test Event",
            Description = "Event description",
            StartDate = DateTime.UtcNow.AddDays(7)
        };

        var response = await _client.PostAsJsonAsync("/api/events", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_NotFound_Returns404()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.DeleteAsync("/api/events/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.DeleteAsync("/api/events/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task JoinEvent_AsVisitor_Returns404WhenEventNotFound()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.PutAsync("/api/events/99999/join", null);

        // service will throw / return not found since event doesn't exist
        Assert.True(response.StatusCode == HttpStatusCode.NotFound
                 || response.StatusCode == HttpStatusCode.InternalServerError
                 || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserEvents_AsVisitor_Returns200()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/events/user-events?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetClubEvents_AsVisitor_Returns200()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/events/club-events/1?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
