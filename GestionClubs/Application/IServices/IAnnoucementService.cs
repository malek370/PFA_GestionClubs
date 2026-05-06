using GestionClubs.Domain.DTOs;

namespace GestionClubs.Application.IServices
{
    public interface IAnnoucementService
    {
        Task<GetAnnoucementDTO> CreateAnnoucement(CreateAnnoucementDTO createAnnoucementDTO);
        Task DeleteAnnoucement(int id);
        Task<GetAnnoucementDTO> GetAnnoucementById(int id);
        Task<IEnumerable<GetAnnoucementDTO>> GetByClubId(int ClubId);
    }
}