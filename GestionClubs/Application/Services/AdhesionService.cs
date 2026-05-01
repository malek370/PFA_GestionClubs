using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.Exceptions;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Application.Services
{
    public class AdhesionService(IBaseRepository<Adhesion> adhesionRepository, IBaseRepository<Member> memberRepository) : IAdhesionService
    {
        private readonly IBaseRepository<Adhesion> _adhesionRepository = adhesionRepository;
        private readonly IBaseRepository<Member> _memberRepository = memberRepository;
        public async Task<IEnumerable<GetAdhesionDTO>> GetAdhesionsByClub(int clubId)
        {
            return await _adhesionRepository.GetAllQueryable()
                .Where(adh => adh.ClubId == clubId)
                .OrderBy(adh => adh.CreatinDate)
                .Select(adh => new GetAdhesionDTO
                {
                    Id = adh.Id,
                    Email = adh.Email,
                    Status = adh.Status.ToString(),
                    ClubName = adh.Club!.Name,
                    FirstName = adh.FirstName,
                    LastName = adh.LastName
                })
                .ToListAsync();
        }
        public async Task<GetAdhesionDTO?> GetAdhesionById(int id)
        {
            var adhesion = await _adhesionRepository.GetById(id);
            return adhesion == null
                ? throw new EntityNotFoundException($"Adhesion with id {id} not found")
                : new GetAdhesionDTO
            {
                Id = adhesion.Id,
                Email = adhesion.Email,
                Status = adhesion.Status.ToString(),
                ClubName = adhesion.Club!.Name,
                FirstName = adhesion.FirstName,
                LastName = adhesion.LastName
            };
        }
        public async Task<GetAdhesionDTO> AddAdhesion(CreateAdhesionDTO adhesionDto)
        {
            if(await _adhesionRepository.GetAllQueryable().AnyAsync(a => a.ClubId == adhesionDto.ClubId && a.Email == "adhesionDto.Email"))
               //TO CHANGE BY TAKING EMAIL FROM TOKEN
               throw new UserAdhesionExistsException("User has already requested to join this club.");
            if(await _memberRepository.GetAllQueryable().AnyAsync(m => m.ClubId == adhesionDto.ClubId && m.Email == "adhesionDto.Email"))
                //TO CHANGE BY TAKING EMAIL FROM TOKEN
                throw new UserAlreadyMemberException("User is already a member of this club.");

            var adhesion = new Adhesion
            {
                ClubId = adhesionDto.ClubId,
                Email = "adhesionDto.Email",//TO CHANGE BY TAKING EMAIL FROM TOKEN
                FirstName = "adhesionDto.FirstName",
                LastName = "adhesionDto.LastName",
                Status = Status.Pending // Assuming a default status
            };
            var addedAdhesion = await _adhesionRepository.Add(adhesion);
            return new GetAdhesionDTO
            {
                Id = addedAdhesion.Id,
                Email = addedAdhesion.Email,
                Status = addedAdhesion.Status.ToString(),
                ClubName = addedAdhesion.Club!.Name,
                FirstName = addedAdhesion.FirstName,
                LastName = addedAdhesion.LastName
            };
        }
        public async Task<GetAdhesionDTO?> AcceptAdhesion(int adhesionId)
        {
            var adhesion = await _adhesionRepository.GetById(adhesionId);
            if (adhesion == null)
            {
                throw new EntityNotFoundException($"Adhesion with id {adhesionId} not found");
            }
            adhesion.Status = Status.Accepted;
            await _memberRepository.Add(new Member
            {
                ClubId = adhesion.ClubId,
                Email = "adhesionDto.Email",//TO CHANGE BY TAKING EMAIL FROM TOKEN
                FirstName = "adhesionDto.FirstName",
                LastName = "adhesionDto.LastName",
                PostInClub = ClubPost.Member // Assuming a default post

            });
            var updatedAdhesion = await _adhesionRepository.Update(adhesion);
            return new GetAdhesionDTO
            {
                Id = updatedAdhesion.Id,
                Email = updatedAdhesion.Email,
                Status = updatedAdhesion.Status.ToString(),
                ClubName = updatedAdhesion.Club!.Name,
                FirstName = updatedAdhesion.FirstName,
                LastName = updatedAdhesion.LastName
            };
        }
        public async Task<GetAdhesionDTO?> RefuseAdhesion(int adhesionId)
        {
            var adhesion = await _adhesionRepository.GetById(adhesionId);
            if (adhesion == null)
            {
                throw new EntityNotFoundException($"Adhesion with id {adhesionId} not found");
            }
            adhesion.Status = Status.Refused;
            var updatedAdhesion = await _adhesionRepository.Update(adhesion);
            return new GetAdhesionDTO
            {
                Id = updatedAdhesion.Id,
                Email = updatedAdhesion.Email,
                Status = updatedAdhesion.Status.ToString(),
                ClubName = updatedAdhesion.Club!.Name,
                FirstName = updatedAdhesion.FirstName,
                LastName = updatedAdhesion.LastName
            };
        }
        public async Task<bool> DeleteAdhesion(int id)
        {
            if(!await _adhesionRepository.GetAllQueryable().AnyAsync(a => a.Id == id))
            {
                throw new EntityNotFoundException($"Adhesion with id {id} not found");
            }
            return await _adhesionRepository.Delete(id);
        }

    }
}
