using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.Services;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Moq;
using MockQueryable.Moq;
using System.Security.Claims;
using MockQueryable;

namespace Application.Test.Services;

public class CurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IBaseRepository<Member>> _memberRepositoryMock;
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _memberRepositoryMock = new Mock<IBaseRepository<Member>>();
        _service = new CurrentUserService(_httpContextAccessorMock.Object, _memberRepositoryMock.Object);
    }

    [Fact]
    public void GetEmail_WithClaimsTypeEmail_ReturnsEmail()
    {
        var email = "test@example.com";
        var claims = new List<Claim> { new Claim(ClaimTypes.Email, email) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var result = _service.GetEmail();

        Assert.Equal(email, result);
    }

    [Fact]
    public void GetEmail_WithEmailClaim_ReturnsEmail()
    {
        var email = "user@test.com";
        var claims = new List<Claim> { new Claim("email", email) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var result = _service.GetEmail();

        Assert.Equal(email, result);
    }

    [Fact]
    public void GetEmail_WithNoClaims_ReturnsNull()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var result = _service.GetEmail();

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckUserIsAdminForClub_UserIsAdmin_DoesNotThrow()
    {
        var email = "admin@test.com";
        var clubId = 1;
        var claims = new List<Claim> { new Claim(ClaimTypes.Email, email) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var members = new List<Member>
        {
            new Member
            {
                ClubId = clubId,
                PostInClub = ClubPost.President,
                User = new User { Email = email, FirstName = "A", LastName = "B" }
            }
        }.BuildMock();

        _memberRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(members);

        await _service.CheckUserIsAdminForClub(clubId);
    }

    [Fact]
    public async Task CheckUserIsAdminForClub_UserNotAuthenticated_ThrowsUnauthorizedException()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        await Assert.ThrowsAsync<AppUnauthorizedException>(() => _service.CheckUserIsAdminForClub(1));
    }

    [Fact]
    public async Task CheckUserIsAdminForClub_UserNotAdmin_ThrowsUnauthorizedException()
    {
        var email = "user@test.com";
        var clubId = 1;
        var claims = new List<Claim> { new Claim(ClaimTypes.Email, email) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var members = new List<Member>
        {
            new Member
            {
                ClubId = clubId,
                PostInClub = ClubPost.Member,
                User = new User { Email = email, FirstName = "A", LastName = "B" }
            }
        }.BuildMock();

        _memberRepositoryMock.Setup(x => x.GetAllQueryable()).Returns(members);

        await Assert.ThrowsAsync<AppUnauthorizedException>(() => _service.CheckUserIsAdminForClub(clubId));
    }
}
