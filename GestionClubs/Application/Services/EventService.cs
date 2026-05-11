using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.Extensions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Application.Services
{
    public class EventService(IBaseRepository<Event> eventRepository, IBaseRepository<User> userRepository, ICurrentUserService currentUserService) : IEventService
    {
        private readonly IBaseRepository<Event> _eventRepository = eventRepository;
        private readonly IBaseRepository<User> _userRepository = userRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        public async Task<GetEventDTO> AddEvent(CreateEventDTO createEventDTO)
        {
            await _currentUserService.CheckUserIsAdminForClub(createEventDTO.ClubId);
            var newEvent = new Event
            {
                ClubId = createEventDTO.ClubId,
                Title = createEventDTO.Title,
                Description = createEventDTO.Description,
                IsPublic = createEventDTO.IsPublic,
                Location = createEventDTO.Location,
                StartDate = createEventDTO.StartDate,
                Tags = createEventDTO.Tags
            };
            var result = await _eventRepository.Add(newEvent);
            return new GetEventDTO
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                Location = result.Location,
                StartDate = result.StartDate,
                ClubName = (await _eventRepository.GetById(result.Id))!.Club!.Name
            };
        }
        public async Task<bool> DeleteEvent(int id)
        {
            var eventToDelete = await _eventRepository.GetById(id)
                ?? throw new EntityNotFoundException($"Event with id {id} not found.");
            await _currentUserService.CheckUserIsAdminForClub(eventToDelete.ClubId);
            return await _eventRepository.Delete(id);
        }
        public async Task<PagedResult<GetEventDTO>> GetEvents(List<string>? tags, PaginationParams pagination)
        {
            var userMail = _currentUserService.GetEmail()!;
            var eventList = _eventRepository.GetAllQueryable()
                .Where(e => e.IsPublic ||
                e.Club!.Members.Any(m => m.User!.Email == userMail))!;
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    eventList = eventList.Where(e => e.Tags.Select(t => t.ToUpper()).Contains(tag.ToUpper()));
                }
            }
            return await eventList
                .OrderBy(e => e.Id)
                .Select(ev => new GetEventDTO
                {
                    Id = ev.Id,
                    Title = ev.Title,
                    Description = ev.Description,
                    Location = ev.Location,
                    StartDate = ev.StartDate,
                    ClubName = ev.Club!.Name
                }).ToPagedResultAsync(pagination);

        }

        public async Task<GetEventDTO> GetEventById(int id)
        {
            var userMail = _currentUserService.GetEmail()!;
            var eventToGet = await _eventRepository.GetAllQueryable()
                .Where(e => e.Id == id && (e.IsPublic || e.Club!.Members.Any(m => m.User!.Email == userMail)))
                .Select(ev => new GetEventDTO
                {
                    Id = ev.Id,
                    Title = ev.Title,
                    Description = ev.Description,
                    Location = ev.Location,
                    StartDate = ev.StartDate,
                    ClubName = ev.Club!.Name
                })
                .FirstOrDefaultAsync()
                ?? throw new EntityNotFoundException($"Event with id {id} not found ");
            return eventToGet;
        }
        public async Task<GetEventDTO> JoinEvent(int eventId)
        {
            var eventToJoin = await _eventRepository.GetById(eventId)
                ?? throw new EntityNotFoundException($"Event with id {eventId} not found.");
            var userEmail = _currentUserService.GetEmail()!;
            if (eventToJoin.Participent.Any(m => m.Email == userEmail))
            {
                throw new InvalidOperationException("User is already a member of this event.");
            }
            if (!(eventToJoin.IsPublic || eventToJoin.Club!.Members.Any(m => m.User!.Email == userEmail)))
            {
                throw new AppUnauthorizedException("User is not authorized to join this event.");
            }
            var user = await _userRepository.GetAllQueryable()
                .FirstOrDefaultAsync(u => u.Email == userEmail)
                ?? throw new EntityNotFoundException($"User with email {userEmail} not found.");
            eventToJoin.Participent.Add(user);
            var result = await _eventRepository.Update(eventToJoin);
            return new GetEventDTO
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                Location = result.Location,
                StartDate = result.StartDate,
                ClubName = (await _eventRepository.GetById(result.Id))!.Club!.Name
            };
        }
        public async Task<GetEventDTO> LeaveEvent(int eventId)
        {
            var eventToLeave = await _eventRepository.GetById(eventId)
                ?? throw new EntityNotFoundException($"Event with id {eventId} not found.");
            var userEmail = _currentUserService.GetEmail()!;
            if (!eventToLeave.Participent.Any(m => m.Email == userEmail))
            {
                throw new InvalidOperationException("User is not a member of this event.");
            }
            var user = await _userRepository.GetAllQueryable()
                .FirstOrDefaultAsync(u => u.Email == userEmail)
                ?? throw new EntityNotFoundException($"User with email {userEmail} not found.");
            eventToLeave.Participent.Remove(user);
            var result = await _eventRepository.Update(eventToLeave);
            return new GetEventDTO
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                Location = result.Location,
                StartDate = result.StartDate,
                ClubName = (await _eventRepository.GetById(result.Id))!.Club!.Name
            };
        }
        
        public async Task<PagedResult<GetEventDTO>> GetUserEvents(PaginationParams pagination)
        {

            var userEmail = _currentUserService.GetEmail()!;
            return await _eventRepository.GetAllQueryable()
                .Where(e => e.Participent.Any(p => p.Email == userEmail))
                .OrderBy(e => e.Id)
                .Select(ev => new GetEventDTO
                {
                    Id = ev.Id,
                    Title = ev.Title,
                    Description = ev.Description,
                    Location = ev.Location,
                    StartDate = ev.StartDate,
                    ClubName = ev.Club!.Name
                }).ToPagedResultAsync(pagination);
        }
        public async Task<PagedResult<GetEventDTO>> GetClubEvents(int clubId, PaginationParams pagination)
        {
            await _currentUserService.CheckUserIsAdminForClub(clubId);
            return await _eventRepository.GetAllQueryable()
                .Where(e => e.ClubId == clubId &&
                (e.IsPublic || e.Club!.Members.Any(m => m.User!.Email == _currentUserService.GetEmail())))
                .OrderBy(e => e.Id)
                .Select(ev => new GetEventDTO
                {
                    Id = ev.Id,
                    Title = ev.Title,
                    Description = ev.Description,
                    Location = ev.Location,
                    StartDate = ev.StartDate,
                    ClubName = ev.Club!.Name
                }).ToPagedResultAsync(pagination);

        }
    }
}
