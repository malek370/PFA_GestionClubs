using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;

namespace GestionClubs.Application.IServices
{
    public interface IMembersService
    {
        Task<GetMemberDTO?> GetMemberById(int id);
        Task<PagedResult<GetMemberDTO>> GetMembersByClub(int clubId, PaginationParams pagination);
        Task<GetMemberDTO> UpdateMemberPost(UpdateMemberPostDTO update);
        Task<bool> RemoveMember(int memberId);
    }
}