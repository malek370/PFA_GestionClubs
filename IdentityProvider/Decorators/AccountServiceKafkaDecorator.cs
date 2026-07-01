using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Events;
using IdentityProvider.Kafka;
using IdentityProvider.Options;
using IdentityProvider.Requests;
using IdentityProvider.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IdentityProvider.Decorators
{
    public class AccountServiceKafkaDecorator : IAccountService
    {
        private readonly IAccountService _inner;
        private readonly IKafkaProducer _producer;
        private readonly IOptions<KafkaOptions> _options;
        private readonly UserManager<User> _userManager;

        public AccountServiceKafkaDecorator(
            IAccountService inner,
            IKafkaProducer producer,
            IOptions<KafkaOptions> options,
            UserManager<User> userManager)
        {
            _inner = inner;
            _producer = producer;
            _options = options;
            _userManager = userManager;
        }

        public async Task RegisterAsync(RegisterRequest registerRequest)
        {
            await _inner.RegisterAsync(registerRequest);

            var user = await _userManager.FindByEmailAsync(registerRequest.Email);
            if (user is not null)
            {
                var @event = new UserRegisteredEvent
                {
                    UserId = user.Id,
                    Email = registerRequest.Email,
                    FirstName = registerRequest.FirstName,
                    LastName = registerRequest.LastName,
                    RegisteredAt = DateTime.UtcNow
                };

                await _producer.PublishAsync(
                    _options.Value.ProducerTopic,
                    user.Id.ToString(),
                    @event);
            }
        }

        public Task<TokenResponse> LoginAsync(LoginRequest loginRequest) => _inner.LoginAsync(loginRequest);
        public Task<TokenResponse> RefreshTokenAsync(string? refreshToken) => _inner.RefreshTokenAsync(refreshToken);
    }
}
