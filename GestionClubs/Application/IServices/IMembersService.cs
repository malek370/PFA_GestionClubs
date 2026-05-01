using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;

namespace GestionClubs.Application.IServices
{
    public interface IMembersService
    {
        Task<GetMemberDTO?> GetMemberById(int id);
        Task<IEnumerable<GetMemberDTO>> GetMembersByClub(int clubId);
        Task<GetMemberDTO> UpdateMemberPost(UpdateMemberPostDTO update);
    }
}