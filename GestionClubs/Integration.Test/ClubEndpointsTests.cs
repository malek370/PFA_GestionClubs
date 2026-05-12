using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;
using System.Net;
using System.Net.Http.Json;

namespace Integration.Test;

public class ClubEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ClubEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetClubs_AsVisitor_Returns200()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/clubs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetClubs_WithNameFilter_Returns200()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/clubs?name=test&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetClubs_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Remove("X-Test-Roles");
        // Remove auth header entirely by creating a fresh unauthenticated request
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/clubs?page=1&pageSize=10");
        request.Headers.Add("X-Test-Roles", "None");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateClub_AsPlatformAdmin_Returns201()
    {
        _client.WithRole(AppRoles.PlatformAdmin);

        var dto = new CreateClubDTO
        {
            Name = "Test Create Club",
            Description = "A test club description",
            Email = "club@test.com"
        };

        var response = await _client.PostAsJsonAsync("/api/clubs", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateClub_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var dto = new CreateClubDTO
        {
            Name = "Test Club",
            Description = "A test club description",
            Email = "club@test.com"
        };
        
        var response = await _client.PostAsJsonAsync("/api/clubs", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateClub_InvalidBody_Returns400()
    {
        _client.WithRole(AppRoles.PlatformAdmin);

        var dto = new { Name = "", Description = "" };

        var response = await _client.PostAsJsonAsync("/api/clubs", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUserClubs_AsClubMember_Returns200()
    {
        _client.WithRole(AppRoles.ClubMember);

        var response = await _client.GetAsync("/api/clubs/user-clubs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserClubs_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/clubs/user-clubs?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
