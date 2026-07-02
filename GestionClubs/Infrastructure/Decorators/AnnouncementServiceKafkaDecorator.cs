using GestionClubs.Application.Events;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Pagination;
using GestionClubs.Infrastructure.Kafka;
using Microsoft.Extensions.Options;

namespace GestionClubs.Infrastructure.Decorators
{
    public class AnnouncementServiceKafkaDecorator : IAnnoucementService
    {
        private readonly IAnnoucementService _inner;
        private readonly IKafkaProducer _producer;
        private readonly IOptions<KafkaOptions> _options;

        public AnnouncementServiceKafkaDecorator(
            IAnnoucementService inner,
            IKafkaProducer producer,
            IOptions<KafkaOptions> options)
        {
            _inner = inner;
            _producer = producer;
            _options = options;
        }

        public async Task<GetAnnoucementDTO> CreateAnnoucement(CreateAnnoucementDTO createAnnoucementDTO)
        {
            var result = await _inner.CreateAnnoucement(createAnnoucementDTO);

            var @event = new AnnouncementCreatedEvent
            {
                AnnouncementId = result.Id,
                Title = result.Title,
                Content = result.Content,
                ClubName = result.ClubName,
                CreatedAt = DateTime.UtcNow
            };

            await _producer.PublishAsync(
                _options.Value.AnnouncementsTopic,
                result.Id.ToString(),
                @event);

            return result;
        }

        public Task DeleteAnnoucement(int id) 
            => _inner.DeleteAnnoucement(id);

        public Task<GetAnnoucementDTO> GetAnnoucementById(int id) 
            => _inner.GetAnnoucementById(id);

        public Task<PagedResult<GetAnnoucementDTO>> GetByClubId(int ClubId, PaginationParams pagination) 
            => _inner.GetByClubId(ClubId, pagination);
    }
}
