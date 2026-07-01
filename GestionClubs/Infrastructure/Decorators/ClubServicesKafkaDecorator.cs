using GestionClubs.Application.Events;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Pagination;
using GestionClubs.Infrastructure.Kafka;
using Microsoft.Extensions.Options;

namespace GestionClubs.Infrastructure.Decorators
{
    public class ClubServicesKafkaDecorator : IClubServices
    {
        private readonly IClubServices _inner;
        private readonly IKafkaProducer _producer;
        private readonly IOptions<KafkaOptions> _options;

        public ClubServicesKafkaDecorator(
            IClubServices inner,
            IKafkaProducer producer,
            IOptions<KafkaOptions> options)
        {
            _inner = inner;
            _producer = producer;
            _options = options;
        }

        public async Task<GetClubDTO> CreateClub(CreateClubDTO createClubDTO)
        {
            var result = await _inner.CreateClub(createClubDTO);

            var @event = new UserPromotedToClubAdminEvent
            {
                Email = createClubDTO.Email,
                ClubId = result.Id,
                PromotedAt = DateTime.UtcNow
            };

            await _producer.PublishAsync(
                _options.Value.ProducerTopic,
                createClubDTO.Email,
                @event);

            return result;
        }

        public Task<PagedResult<GetClubDTO>> GetClubs(FilterClubDTO filter, PaginationParams pagination) 
            => _inner.GetClubs(filter, pagination);

        public Task<PagedResult<GetUserClub>> GetUserClubs(PaginationParams pagination) 
            => _inner.GetUserClubs(pagination);
    }
}
