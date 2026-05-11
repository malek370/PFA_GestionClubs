using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.Exceptions;
using GestionClubs.Application.Extensions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using GestionClubs.Domain.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GestionClubs.Application.Services
{
    public class ClubServices(IBaseRepository<Club> clubRepository,
        IBaseRepository<User> userRepository,
        ICurrentUserService currentUserService) : IClubServices
    {
        public async Task<GetClubDTO> CreateClub(CreateClubDTO createClubDTO)
        {

            if (await clubRepository.GetAllQueryable().AnyAsync(c => c.Name.ToUpper() == createClubDTO.Name.ToUpper()))
            {
                throw new ClubExistsException("Club with the same name already exists");
            }
            var club = new Club
            {
                Name = createClubDTO.Name,
                Description = createClubDTO.Description,
                Members = new Collection<Member>(),
                Documents = createClubDTO.Documents
            };
            var user = await userRepository.GetAllQueryable().FirstOrDefaultAsync(u => u.Email == createClubDTO.Email) ?? throw new EntityNotFoundException("User not found");
            club.Members.Add(new Member
            {

                User = user,
                PostInClub = ClubPost.President
            });
            var addedClub = await clubRepository.Add(club);
            return new GetClubDTO
            {
                Id = addedClub.Id,
                Name = addedClub.Name,
                Description = addedClub.Description,
                PresidentMail = user.Email,
            };
        }
        public async Task<PagedResult<GetClubDTO>> GetClubs(FilterClubDTO filter, PaginationParams pagination)
        {
            var clubs = clubRepository.GetAllQueryable();

            if (!string.IsNullOrEmpty(filter.Name))
            {
                clubs = clubs.Where(c => c.Name.ToUpper().Contains(filter.Name.ToUpper()));
            }

            if (!string.IsNullOrEmpty(filter.Description))
            {
                clubs = clubs.Where(c => c.Description.ToUpper().Contains(filter.Description.ToUpper()));
            }

            return await clubs.OrderBy(c => c.Id).Select(c => new GetClubDTO
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                PresidentMail = c.Members.FirstOrDefault(m => m.PostInClub == ClubPost.President)!.User!.Email
            }).ToPagedResultAsync(pagination);
        }
        public async Task<PagedResult<GetUserClub>> GetUserClubs(PaginationParams pagination)
        {
            var userEmail = currentUserService.GetEmail() ?? throw new UnauthorizedAccessException("User is not authenticated");
            return await clubRepository.GetAllQueryable()
                .Where(c => c.Members.Any(m => m.User!.Email == userEmail))
                .OrderBy(c => c.Id)
                .Select(c => new GetUserClub
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    PresidentMail = c.Members.FirstOrDefault(m => m.PostInClub == ClubPost.President)!.User!.Email,
                    UserPost = c.Members.FirstOrDefault(m => m.User!.Email == userEmail)!.PostInClub.ToString()
                }).ToPagedResultAsync(pagination);
        }
    }
}
