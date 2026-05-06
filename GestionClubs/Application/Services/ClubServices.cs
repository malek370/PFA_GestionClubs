using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.Exceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GestionClubs.Application.Services
{
    public class ClubServices(IBaseRepository<Club> clubRepository,
        IBaseRepository<User> userRepository) : IClubServices
    {
        public async Task<GetClubDTO> CreateClub(CreateClubDTO createClubDTO)
        {

            if(await clubRepository.GetAllQueryable().AnyAsync(c => c.Name.ToUpper() == createClubDTO.Name.ToUpper()))
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
                PresidentMail = c.Members.FirstOrDefault(m => m.PostInClub == ClubPost.President)!.User!.Email
            }).ToListAsync();
        }
    }
}
