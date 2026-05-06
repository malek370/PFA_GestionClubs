using System.Security.Claims;
using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using GestionClubs.Domain.Enums;
namespace GestionClubs.Application.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, IBaseRepository<Member> memberRepository) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IBaseRepository<Member> _memberRepository = memberRepository;

    public string? GetEmail()
    {
        return _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type == "email")?.Value;
    }
    public async Task CheckUserIsAdminForClub(int clubId)
    {
        var currentUserMail = GetEmail() ?? throw new AppUnauthorizedException("User is not authenticated.");
        var check =  await _memberRepository.GetAllQueryable()
                    .AnyAsync(m=>m.ClubId == clubId && m.User!.Email == currentUserMail && m.PostInClub != ClubPost.Member);
        if (!check) {
            throw new AppUnauthorizedException("User is not an admin for this club.");
        }
    }
}
