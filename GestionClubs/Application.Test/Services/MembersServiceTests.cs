using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Application.Services;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using GestionClubs.Domain.Pagination;
using Moq;

namespace Application.Test.Services;

public class MembersServiceTests
{
    private readonly Mock<IBaseRepository<Member>> _memberRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly MembersService _service;

    public MembersServiceTests()
    {
        _memberRepositoryMock = new Mock<IBaseRepository<Member>>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new MembersService(_memberRepositoryMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetMemberById_MemberNotFound_ThrowsEntityNotFoundException()
    {
        _memberRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Member)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.GetMemberById(1));
    }

    [Fact]
    public async Task GetMemberById_ValidMember_ReturnsMember()
    {
        var member = new Member
        {
            Id = 1,
            ClubId = 1,
            PostInClub = ClubPost.Member,
            Club = new Club { Name = "Test Club", Description = "Desc" },
            User = new User { Email = "user@test.com", FirstName = "John", LastName = "Doe" }
        };

        _memberRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(member);
        _currentUserServiceMock.Setup(x => x.CheckUserIsAdminForClub(It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await _service.GetMemberById(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Club", result.ClubName);
        Assert.Equal("user@test.com", result.User.Email);
    }

    [Fact]
    public async Task UpdateMemberPost_MemberNotFound_ThrowsEntityNotFoundException()
    {
        var updateDto = new UpdateMemberPostDTO { MemberId = 1, NewPost = ClubPost.President };

        _memberRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Member)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.UpdateMemberPost(updateDto));
    }

    [Fact]
    public async Task UpdateMemberPost_ValidUpdate_ReturnsUpdatedMember()
    {
        var member = new Member
        {
            Id = 1,
            ClubId = 1,
            PostInClub = ClubPost.Member,
            Club = new Club { Name = "Test Club", Description = "Desc" },
            User = new User { Email = "user@test.com", FirstName = "John", LastName = "Doe" }
        };

        var updateDto = new UpdateMemberPostDTO { MemberId = 1, NewPost = ClubPost.President };

        _memberRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(member);
        _currentUserServiceMock.Setup(x => x.CheckUserIsAdminForClub(It.IsAny<int>())).Returns(Task.CompletedTask);
        _memberRepositoryMock.Setup(x => x.Update(It.IsAny<Member>())).ReturnsAsync((Member m) => m);

        var result = await _service.UpdateMemberPost(updateDto);

        Assert.NotNull(result);
        Assert.Equal("President", result.PostInClub);
    }

    [Fact]
    public async Task RemoveMember_MemberNotFound_ThrowsEntityNotFoundException()
    {
        _memberRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync((Member)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.RemoveMember(1));
    }

    [Fact]
    public async Task RemoveMember_ValidMember_ReturnsTrue()
    {
        var member = new Member
        {
            Id = 1,
            ClubId = 1,
            PostInClub = ClubPost.Member
        };

        _memberRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).ReturnsAsync(member);
        _currentUserServiceMock.Setup(x => x.CheckUserIsAdminForClub(It.IsAny<int>())).Returns(Task.CompletedTask);
        _memberRepositoryMock.Setup(x => x.Delete(It.IsAny<int>())).ReturnsAsync(true);

        var result = await _service.RemoveMember(1);

        Assert.True(result);
    }
}
