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
    public class AnnoucementService(IBaseRepository<Annoucement> annoucementRepository, IBaseRepository<Club> clubRepository, ICurrentUserService currentUserService) : IAnnoucementService
    {
        public async Task<PagedResult<GetAnnoucementDTO>> GetByClubId(int ClubId, PaginationParams pagination)
        {
            return await annoucementRepository.GetAllQueryable()
                .Where(ann => ann.ClubId == ClubId
                              && (ann.IsPublic || ann.Club!.Members.Any(m => m.User!.Email ==currentUserService.GetEmail() )))
                .OrderBy(ann => ann.Id)
                .Select(x => new GetAnnoucementDTO
                {
                    Id = x.Id,
                    ClubName = x.Club!.Name,
                    Title = x.Title,
                    Content = x.Content
                })
                .ToPagedResultAsync(pagination);
        }
        public async Task<GetAnnoucementDTO> GetAnnoucementById(int id)
        {
            var annoucement = await annoucementRepository.GetAllQueryable()
                .Where(ann => ann.Id == id && (ann.IsPublic || ann.Club!.Members.Any(m => m.User!.Email == currentUserService.GetEmail())))
                .FirstOrDefaultAsync() ?? throw new EntityNotFoundException($"Annoucement with ID {id} does not exist");
            return new GetAnnoucementDTO
            {
                Id = annoucement.Id,
                ClubName = annoucement.Club!.Name,
                Title = annoucement.Title,
                Content = annoucement.Content
            };
        }

        public async Task DeleteAnnoucement(int id)
        {
            var annoucement = await annoucementRepository.GetById(id) ?? throw new EntityNotFoundException($"Annoucement with ID {id} does not exist");
            await currentUserService.CheckUserIsAdminForClub(annoucement.ClubId);
            await annoucementRepository.Delete(id);
        }
        public async Task<GetAnnoucementDTO> CreateAnnoucement(CreateAnnoucementDTO createAnnoucementDTO)
        {
            var club = await clubRepository.GetById(createAnnoucementDTO.ClubId) 
                ?? throw new EntityNotFoundException($"Club with ID {createAnnoucementDTO.ClubId} does not exist");
            await currentUserService.CheckUserIsAdminForClub(club.Id);
            var annoucement = new Annoucement
            {
                Title = createAnnoucementDTO.Title,
                Content = createAnnoucementDTO.Content,
                ClubId = createAnnoucementDTO.ClubId,
                IsPublic = createAnnoucementDTO.IsPublic
            };
            var addedAnnoucement = await annoucementRepository.Add(annoucement);
            return new GetAnnoucementDTO
            {
                Id = addedAnnoucement.Id,
                ClubName = club.Name,
                Title = addedAnnoucement.Title,
                Content = addedAnnoucement.Content
            };
        }

    }
}
