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
    public class AdhesionService(IBaseRepository<Adhesion> adhesionRepository, 
        IBaseRepository<Member> memberRepository,
        IBaseRepository<User> userRepository) : IAdhesionService
    {
        private readonly IBaseRepository<Adhesion> _adhesionRepository = adhesionRepository;
        private readonly IBaseRepository<Member> _memberRepository = memberRepository;
        private readonly IBaseRepository<User> _userRepository = userRepository;
        public async Task<IEnumerable<GetAdhesionDTO>> GetAdhesionsByClub(int clubId)
        {
            return await _adhesionRepository.GetAllQueryable()
                .Where(adh => adh.ClubId == clubId)
                .OrderBy(adh => adh.CreatinDate)
                .Select(adh => new GetAdhesionDTO
                {
                    Id = adh.Id,
                    User = new UserDTO
                    {
                        FirstName = adh.User!.FirstName,
                        LastName = adh.User!.LastName,
                        Email = adh.User!.Email
                    },
                    Status = adh.Status.ToString(),
                    ClubName = adh.Club!.Name,
                })
                .ToListAsync();
        }
        public async Task<GetAdhesionDTO?> GetAdhesionById(int id)
        {
            var adhesion = await _adhesionRepository.GetAllQueryable()
                .Include(adh => adh.Club)
                .Select(adh => new GetAdhesionDTO
                {
                    Id = adh.Id,
                    Status = adh.Status.ToString(),
                    ClubName = adh.Club!.Name,
                    User = new UserDTO
                    {
                        FirstName = adh.User!.FirstName,
                        LastName = adh.User!.LastName,
                        Email = adh.User!.Email
                    }
                })
                .FirstOrDefaultAsync(adh => adh.Id == id);
             return adhesion ?? 
                throw new EntityNotFoundException($"Adhesion with id {id} not found");
        }
        public async Task<GetAdhesionDTO> AddAdhesion(CreateAdhesionDTO adhesionDto)
        {
            var emailTmp = "alice@example.com"; //TO CHANGE BY TAKING EMAIL FROM TOKEN
            if (await _adhesionRepository.GetAllQueryable().AnyAsync(a => a.ClubId == adhesionDto.ClubId && a.User!.Email == emailTmp))
               throw new UserAdhesionExistsException("User has already requested to join this club.");
            if(await _memberRepository.GetAllQueryable().AnyAsync(m => m.ClubId == adhesionDto.ClubId && m.User!.Email == emailTmp))
                throw new UserAlreadyMemberException("User is already a member of this club.");
            var user = await _userRepository.GetAllQueryable().FirstOrDefaultAsync(u => u.Email == emailTmp);
            if (user == null)throw new EntityNotFoundException($"User with email {emailTmp} not found");
            var adhesion = new Adhesion
            {
                ClubId = adhesionDto.ClubId,
                User = user,
                Status = Status.Pending // Assuming a default status
            };
            var addedAdhesion = await _adhesionRepository.Add(adhesion);
            return new GetAdhesionDTO
            {
                Id = addedAdhesion.Id,
                Status = addedAdhesion.Status.ToString(),
                ClubName = addedAdhesion.Club!.Name,
                User = new UserDTO
                {
                    FirstName = addedAdhesion.User!.FirstName,
                    LastName = addedAdhesion.User!.LastName,
                    Email = addedAdhesion.User!.Email
                }
            };
        }
        public async Task<GetAdhesionDTO?> AcceptAdhesion(int adhesionId)
        {
            var adhesion = await _adhesionRepository.GetById(adhesionId);
            if (adhesion == null)
            {
                throw new EntityNotFoundException($"Adhesion with id {adhesionId} not found");
            }
            if( await _memberRepository.GetAllQueryable()
                .AnyAsync(m => m.ClubId == adhesion.ClubId && m.User!.Email == adhesion.User!.Email))
            {
                throw new UserAlreadyMemberException("User is already a member of this club.");
            }
            var user = await _userRepository.GetAllQueryable().FirstOrDefaultAsync(u => u.Id == adhesion.UserId) ?? throw new EntityNotFoundException($"User with id {adhesion.UserId} not found");
            adhesion.Status = Status.Accepted;
            await _memberRepository.Add(new Member
            {
                ClubId = adhesion.ClubId,
                UserId = adhesion.UserId,
                PostInClub = ClubPost.Member // Assuming a default post

            });
            var updatedAdhesion = await _adhesionRepository.Update(adhesion);
            return new GetAdhesionDTO
            {
                Id = updatedAdhesion.Id,
                Status = updatedAdhesion.Status.ToString(),
                ClubName = updatedAdhesion.Club!.Name,
                User = new UserDTO
                {
                    FirstName = updatedAdhesion.User!.FirstName,
                    LastName = updatedAdhesion.User!.LastName,
                    Email = updatedAdhesion.User!.Email
                }
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
            if(await _memberRepository.GetAllQueryable()
                .AnyAsync(m => m.ClubId == adhesion.ClubId && m.User!.Email == adhesion.User!.Email))
            {
                throw new UserAlreadyMemberException("User is already a member of this club.");
            }
            var user = await _userRepository.GetAllQueryable().FirstOrDefaultAsync(u => u.Id == adhesion.UserId) ?? throw new EntityNotFoundException($"User with id {adhesion.UserId} not found");

            var updatedAdhesion = await _adhesionRepository.Update(adhesion);
            return new GetAdhesionDTO
            {
                Id = updatedAdhesion.Id,
                Status = updatedAdhesion.Status.ToString(),
                ClubName = updatedAdhesion.Club!.Name,
                User = new UserDTO
                {
                    FirstName = updatedAdhesion.User!.FirstName,
                    LastName = updatedAdhesion.User!.LastName,
                    Email = updatedAdhesion.User!.Email
                }
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
