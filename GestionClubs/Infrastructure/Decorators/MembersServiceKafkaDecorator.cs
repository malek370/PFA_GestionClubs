using GestionClubs.Application.Events;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Enums;
using GestionClubs.Domain.Pagination;
using GestionClubs.Infrastructure.Kafka;
using Microsoft.Extensions.Options;

namespace GestionClubs.Infrastructure.Decorators
{
    public class MembersServiceKafkaDecorator : IMembersService
    {
        private readonly IMembersService _inner;
        private readonly IKafkaProducer _producer;
        private readonly IOptions<KafkaOptions> _options;
        private readonly IBaseRepository<Member> _memberRepository;

        public MembersServiceKafkaDecorator(
            IMembersService inner,
            IKafkaProducer producer,
            IOptions<KafkaOptions> options,
            IBaseRepository<Member> memberRepository)
        {
            _inner = inner;
            _producer = producer;
            _options = options;
            _memberRepository = memberRepository;
        }

        public async Task<GetMemberDTO> UpdateMemberPost(UpdateMemberPostDTO update)
        {
            var result = await _inner.UpdateMemberPost(update);

            if (update.NewPost == ClubPost.President)
            {
                var member = await _memberRepository.GetById(update.MemberId);
                var @event = new UserPromotedToClubAdminEvent
                {
                    Email = result.User!.Email,
                    ClubId = member?.ClubId ?? 0,
                    PromotedAt = DateTime.UtcNow
                };

                await _producer.PublishAsync(
                    _options.Value.ProducerTopic,
                    result.User.Email,
                    @event);
            }

            return result;
        }

        public Task<GetMemberDTO?> GetMemberById(int id) => _inner.GetMemberById(id);
        public Task<PagedResult<GetMemberDTO>> GetMembersByClub(int clubId, PaginationParams pagination) => _inner.GetMembersByClub(clubId, pagination);
        public Task<bool> RemoveMember(int memberId) => _inner.RemoveMember(memberId);
    }
}
