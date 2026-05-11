using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;

namespace GestionClubs.Application.IServices
{
    public interface IEventService
    {
        Task<GetEventDTO> AddEvent(CreateEventDTO createEventDTO);
        Task<bool> DeleteEvent(int id);
        Task<PagedResult<GetEventDTO>> GetEvents(List<string>? tags, PaginationParams pagination);
        Task<GetEventDTO> JoinEvent(int eventId);
        Task<GetEventDTO> LeaveEvent(int eventId);
        Task<GetEventDTO> GetEventById(int id);
        Task<PagedResult<GetEventDTO>> GetUserEvents(PaginationParams pagination);
        Task<PagedResult<GetEventDTO>> GetClubEvents(int clubId, PaginationParams pagination);
    }
}