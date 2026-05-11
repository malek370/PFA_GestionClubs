using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;

namespace GestionClubs.Application.IServices
{
    public interface IClubServices
    {
        Task<GetClubDTO> CreateClub(CreateClubDTO createClubDTO);
        Task<PagedResult<GetClubDTO>> GetClubs(FilterClubDTO filter, PaginationParams pagination);
        Task<PagedResult<GetUserClub>> GetUserClubs(PaginationParams pagination);
    }
}