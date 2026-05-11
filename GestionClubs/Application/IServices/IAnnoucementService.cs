using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Pagination;

namespace GestionClubs.Application.IServices
{
    public interface IAnnoucementService
    {
        Task<GetAnnoucementDTO> CreateAnnoucement(CreateAnnoucementDTO createAnnoucementDTO);
        Task DeleteAnnoucement(int id);
        Task<GetAnnoucementDTO> GetAnnoucementById(int id);
        Task<PagedResult<GetAnnoucementDTO>> GetByClubId(int ClubId, PaginationParams pagination);
    }
}