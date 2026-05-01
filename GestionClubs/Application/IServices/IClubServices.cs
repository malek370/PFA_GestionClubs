using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;

namespace GestionClubs.Application.IServices
{
    public interface IClubServices
    {
        Task<GetClubDTO> CreateClub(CreateClubDTO createClubDTO);
        Task<List<GetClubDTO>> GetClubs(FilterClubDTO filter);
    }
}