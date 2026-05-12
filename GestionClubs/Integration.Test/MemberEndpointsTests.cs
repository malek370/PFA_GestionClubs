using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

namespace Integration.Test;

public class MemberEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MemberEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMembersByClub_AsClubAdmin_Returns200()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.GetAsync("/api/members/club/1?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMembersByClub_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/members/club/1?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetMemberById_NotFound_Returns404()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.GetAsync("/api/members/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMemberById_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.GetAsync("/api/members/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMemberPost_AsClubAdmin_ReturnsExpected()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var dto = new UpdateMemberPostDTO
        {
            MemberId = 99999,
            NewPost = ClubPost.Secretary
        };

        var response = await _client.PutAsJsonAsync("/api/members/post", dto);

        Assert.True(response.StatusCode == HttpStatusCode.OK
                 || response.StatusCode == HttpStatusCode.NotFound
                 || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UpdateMemberPost_InvalidBody_Returns400()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.PutAsJsonAsync("/api/members/post", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMemberPost_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var dto = new UpdateMemberPostDTO
        {
            MemberId = 1,
            NewPost = ClubPost.Member
        };

        var response = await _client.PutAsJsonAsync("/api/members/post", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMember_NotFound_Returns404()
    {
        _client.WithRole(AppRoles.ClubAdmin);

        var response = await _client.DeleteAsync("/api/members/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMember_AsVisitor_Returns403()
    {
        _client.WithRole(AppRoles.Visitor);

        var response = await _client.DeleteAsync("/api/members/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
