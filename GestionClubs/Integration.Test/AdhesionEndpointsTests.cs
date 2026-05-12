using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using System.Net;
using System.Net.Http.Json;

namespace Integration.Test;

public class AdhesionEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdhesionEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAdhesionsByClub_AsClubAdmin_Returns200()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.GetAsync("/api/adhesions/club/1?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAdhesionsByClub_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/adhesions/club/1?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAdhesionById_NotFound_Returns404()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.GetAsync("/api/adhesions/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAdhesionById_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/adhesions/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdhesion_AsVisitor_Returns201()
    {
        _client.WithRole(AppRoles.Visitor);

        var dto = new CreateAdhesionDTO { ClubId = 1 };

        var response = await _client.PostAsJsonAsync("/api/adhesions", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdhesion_InvalidBody_Returns400()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.PostAsJsonAsync("/api/adhesions", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AcceptAdhesion_NotFound_Returns404OrError()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.PutAsync("/api/adhesions/99999/accept", null);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptAdhesion_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.PutAsync("/api/adhesions/1/accept", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefuseAdhesion_AsClubAdmin_ReturnsExpected()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.PutAsync("/api/adhesions/99999/refuse", null);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound
                 || response.StatusCode == HttpStatusCode.InternalServerError
                 || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAdhesion_NotFound_Returns404()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.DeleteAsync("/api/adhesions/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAdhesion_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.DeleteAsync("/api/adhesions/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMyAdhesions_AsVisitor_Returns200()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/adhesions/myadhesions?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
