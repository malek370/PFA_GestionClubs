using GestionClubs.Application.BaseExceptions;
using GestionClubs.Application.Exceptions;
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
    public class MembersService(IBaseRepository<Member> _memberRepository) : IMembersService
    {
        public async Task<IEnumerable<GetMemberDTO>> GetMembersByClub(int clubId)
        {
            return await _memberRepository.GetAllQueryable()
                .Where(member => member.ClubId == clubId)
                .Select(member => new GetMemberDTO
                {
                    Id = member.Id,
                    ClubName = member.Club!.Name,
                    FirstName = member.FirstName,
                    LastName = member.LastName,
                    Email = member.Email,
                    PostInClub = member.PostInClub.ToString()
                })
                .ToListAsync();
        }
        public async Task<GetMemberDTO?> GetMemberById(int id)
        {
            var member = await _memberRepository.GetById(id);
            if (member == null)
            {
                throw new EntityNotFoundException($"Member with ID {id} not found.");
            }
            return new GetMemberDTO
            {
                Id = member.Id,
                ClubName = member.Club!.Name,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                PostInClub = member.PostInClub.ToString()
            };
        }
        public async Task<GetMemberDTO> UpdateMemberPost(UpdateMemberPostDTO update)
        {
            var member = await _memberRepository.GetById(update.MemberId);
            if (member == null)
            {
                throw new EntityNotFoundException($"Member with ID {update.MemberId} not found.");
            }
            member.PostInClub = update.NewPost;
            await _memberRepository.Update(member);
            return new GetMemberDTO
            {
                Id = member.Id,
                ClubName = member.Club!.Name,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                PostInClub = member.PostInClub.ToString()
            };
        }
        public async Task<bool> RemoveMember(int memberId)
        {
            var member = await _memberRepository.GetById(memberId);
            if (member == null)
            {
                throw new EntityNotFoundException($"Member with ID {memberId} not found.");
            }
            return await _memberRepository.Delete(memberId);
        }
    }
}
