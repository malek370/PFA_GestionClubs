using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;

namespace GestionClubs.Application.IServices
{
    public interface IAdhesionService
    {
        Task<GetAdhesionDTO?> AcceptAdhesion(int adhesionId);
        Task<GetAdhesionDTO> AddAdhesion(CreateAdhesionDTO adhesionDto);
        Task<bool> DeleteAdhesion(int id);
        Task<GetAdhesionDTO?> GetAdhesionById(int id);
        Task<IEnumerable<GetAdhesionDTO>> GetAdhesionsByClub(int clubId);
        Task<GetAdhesionDTO?> RefuseAdhesion(int adhesionId);
        Task<IEnumerable<GetAdhesionDTO>> GetAdhesionsByUser();
    }
}