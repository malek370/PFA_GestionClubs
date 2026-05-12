using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using System.Net;
using System.Net.Http.Json;

namespace Integration.Test;

public class AnnouncementEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnnouncementEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAnnouncementsByClub_AsClubAdmin_Returns200()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.GetAsync("/api/annoucements/club/1?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAnnouncementsByClub_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/annoucements/club/1?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAnnouncementById_AsClubMember_Returns404WhenNotExist()
    {
        _client.WithRole(AppRoles.ClubMember);

        var response = await _client.GetAsync("/api/annoucements/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAnnouncementById_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/annoucements/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAnnouncement_AsClubAdmin_Returns201()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var dto = new CreateAnnoucementDTO
        {
            Title = "Test Announcement",
            Content = "This is the announcement content.",
            ClubId = 1,
            IsPublic = true
        };

        var response = await _client.PostAsJsonAsync("/api/annoucements", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateAnnouncement_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var dto = new CreateAnnoucementDTO
        {
            Title = "Test Announcement",
            Content = "Content",
            ClubId = 1
        };

        var response = await _client.PostAsJsonAsync("/api/annoucements", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAnnouncement_AsClubAdmin_Returns204()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.DeleteAsync("/api/annoucements/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAnnouncement_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.DeleteAsync("/api/annoucements/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
