using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;

namespace GestionClubs.Application.IServices
{
    public interface IAdhesionService
    {
        Task<GetAdhesionDTO?> AcceptAdhesion(int adhesionId);
        Task<GetAdhesionDTO> AddAdhesion(CreateAdhesionDTO adhesionDto);
        Task<bool> DeleteAdhesion(int id);
        Task<GetAdhesionDTO?> GetAdhesionById(int id);
        Task<PagedResult<GetAdhesionDTO>> GetAdhesionsByClub(int clubId, PaginationParams pagination);
        Task<GetAdhesionDTO?> RefuseAdhesion(int adhesionId);
        Task<PagedResult<GetAdhesionDTO>> GetAdhesionsByUser(PaginationParams pagination);
    }
}