using GestionClubs.Application.Events;
using GestionClubs.Application.IRepositories;
using GestionClubs.Application.IServices;
using GestionClubs.Domain.DTOs;
using GestionClubs.Domain.Entities;
using GestionClubs.Domain.Pagination;
using GestionClubs.Infrastructure.Kafka;
using Microsoft.Extensions.Options;

namespace GestionClubs.Infrastructure.Decorators
{
    public class AdhesionServiceKafkaDecorator : IAdhesionService
    {
        private readonly IAdhesionService _inner;
        private readonly IKafkaProducer _producer;
        private readonly IOptions<KafkaOptions> _options;
        private readonly IBaseRepository<Adhesion> _adhesionRepository;

        public AdhesionServiceKafkaDecorator(
            IAdhesionService inner,
            IKafkaProducer producer,
            IOptions<KafkaOptions> options,
            IBaseRepository<Adhesion> adhesionRepository)
        {
            _inner = inner;
            _producer = producer;
            _options = options;
            _adhesionRepository = adhesionRepository;
        }

        public async Task<GetAdhesionDTO?> AcceptAdhesion(int adhesionId)
        {
            var adhesion = await _adhesionRepository.GetById(adhesionId);
            var result = await _inner.AcceptAdhesion(adhesionId);

            if (result != null && adhesion != null)
            {
                var @event = new UserPromotedToClubMemberEvent
                {
                    Email = result.User!.Email,
                    ClubId = adhesion.ClubId,
                    PromotedAt = DateTime.UtcNow
                };

                await _producer.PublishAsync(
                    _options.Value.ProducerTopicMember,
                    result.User.Email,
                    @event);
            }

            return result;
        }

        public Task<GetAdhesionDTO> AddAdhesion(CreateAdhesionDTO adhesionDto)
            => _inner.AddAdhesion(adhesionDto);

        public Task<bool> DeleteAdhesion(int id)
            => _inner.DeleteAdhesion(id);

        public Task<GetAdhesionDTO?> GetAdhesionById(int id)
            => _inner.GetAdhesionById(id);

        public Task<PagedResult<GetAdhesionDTO>> GetAdhesionsByClub(int clubId, PaginationParams pagination)
            => _inner.GetAdhesionsByClub(clubId, pagination);

        public Task<PagedResult<GetAdhesionDTO>> GetAdhesionsByUser(PaginationParams pagination)
            => _inner.GetAdhesionsByUser(pagination);

        public Task<GetAdhesionDTO?> RefuseAdhesion(int adhesionId)
            => _inner.RefuseAdhesion(adhesionId);
    }
}
