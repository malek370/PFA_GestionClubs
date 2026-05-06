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
    public class MembersService(IBaseRepository<Member> _memberRepository,ICurrentUserService currentUserService) : IMembersService
    {
        public async Task<IEnumerable<GetMemberDTO>> GetMembersByClub(int clubId)
        {
            await currentUserService.CheckUserIsAdminForClub(clubId);
            var members = await _memberRepository.GetAllQueryable()
                .Where(member => member.ClubId == clubId)
                .Select(member => new GetMemberDTO
                {
                    Id = member.Id,
                    ClubName = member.Club!.Name,
                    User = new UserDTO
                    {
                        FirstName = member.User!.FirstName,
                        LastName = member.User!.LastName,
                        Email = member.User!.Email
                    },
                    PostInClub = member.PostInClub.ToString()
                })
                .ToListAsync();
            return members;
        }
        public async Task<GetMemberDTO?> GetMemberById(int id)
        {
            var member = await _memberRepository.GetById(id) ?? throw new EntityNotFoundException($"Member with ID {id} not found.");
            await currentUserService.CheckUserIsAdminForClub(member.ClubId);
            return new GetMemberDTO
            {
                Id = member.Id,
                ClubName = member.Club!.Name,
                User = new UserDTO
                {
                    FirstName = member.User!.FirstName,
                    LastName = member.User!.LastName,
                    Email = member.User!.Email
                },
                PostInClub = member.PostInClub.ToString()
            };
        }
        public async Task<GetMemberDTO> UpdateMemberPost(UpdateMemberPostDTO update)
        {
            var member = await _memberRepository.GetById(update.MemberId) ?? throw new EntityNotFoundException($"Member with ID {update.MemberId} not found.");
            await currentUserService.CheckUserIsAdminForClub(member.ClubId);
            member.PostInClub = update.NewPost;
            var updatedMember = await _memberRepository.Update(member);
            return new GetMemberDTO
            {
                Id = updatedMember.Id,
                ClubName = updatedMember.Club!.Name,
                User = new UserDTO
                {
                    FirstName = updatedMember.User!.FirstName,
                    LastName = updatedMember.User!.LastName,
                    Email = updatedMember.User!.Email
                },
                PostInClub = updatedMember.PostInClub.ToString()
            };
        }
        public async Task<bool> RemoveMember(int memberId)
        {
            var member = await _memberRepository.GetById(memberId) ?? throw new EntityNotFoundException($"Member with ID {memberId} not found.");
            await currentUserService.CheckUserIsAdminForClub(member.ClubId);
            return await _memberRepository.Delete(memberId);
        }
    }
}
