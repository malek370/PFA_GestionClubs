using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Application.Services
{
    public class AnnoucementService(IBaseRepository<Annoucement> annoucementRepository, IBaseRepository<Club> clubRepository) : IAnnoucementService
    {
        public async Task<IEnumerable<GetAnnoucementDTO>> GetByClubId(int ClubId)
        {
            return await annoucementRepository.GetAllQueryable()
                .Where(ann => ann.ClubId == ClubId)
                .Select(x => new GetAnnoucementDTO
                {
                    Id = x.Id,
                    ClubName = x.Club!.Name,
                    Title = x.Title,
                    Content = x.Content
                })
                .ToListAsync();
        }
        public async Task<GetAnnoucementDTO> GetAnnoucementById(int id)
        {
            var annoucement = await annoucementRepository.GetById(id);
            if (annoucement == null) throw new EntityNotFoundException($"Annoucement with ID {id} does not exist");
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
            await annoucementRepository.Delete(id);
        }
        public async Task<GetAnnoucementDTO> CreateAnnoucement(CreateAnnoucementDTO createAnnoucementDTO)
        {
            var club = await clubRepository.GetById(createAnnoucementDTO.ClubId) 
                ?? throw new EntityNotFoundException($"Club with ID {createAnnoucementDTO.ClubId} does not exist");
            var annoucement = new Annoucement
            {
                Title = createAnnoucementDTO.Title,
                Content = createAnnoucementDTO.Content,
                ClubId = createAnnoucementDTO.ClubId
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
