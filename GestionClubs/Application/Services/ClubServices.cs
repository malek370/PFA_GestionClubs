using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GestionClubs.Application.Services
{
    public class ClubServices(IBaseRepository<Club> clubRepository, IHttpContextAccessor httpContextAccessor) : IClubServices
    {
        public async Task<GetClubDTO> CreateClub(CreateClubDTO createClubDTO)
        {

            if (!httpContextAccessor.HttpContext.Request
                .Headers["Roles"].ToString().Split(';').Contains("AdminPlatform"))
                throw new UnauthorizedAccessException("Unauthorized to create a club");
            var club = new Club
            {
                Name = createClubDTO.Name,
                Description = createClubDTO.Description,
                Members = new Collection<Member>(),
            };
            club.Members.Add(new Member
            {

                FirstName = createClubDTO.FirstName,
                LastName = createClubDTO.Name,
                Email = createClubDTO.Email,
                PostInClub = ClubPost.President
            });
            var addedClub = await clubRepository.Add(club);
            return new GetClubDTO
            {
                Id = addedClub.Id,
                Name = addedClub.Name,
                Description = addedClub.Description,
                Documents = addedClub.Documents
            };
        }
        public async Task<List<GetClubDTO>> GetClubs(FilterClubDTO filter)
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

            return await clubs.Select(c => new GetClubDTO
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Documents = c.Documents
            }).ToListAsync();
        }
    }
}
