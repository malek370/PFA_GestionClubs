using Confluent.Kafka;
using IdentityProvider.Entities;
using IdentityProvider.Events;
using IdentityProvider.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IdentityProvider.Consumers
{
    public class UserPromotedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<KafkaOptions> _options;
        private readonly ILogger<UserPromotedConsumer> _logger;

        public UserPromotedConsumer(
            IServiceScopeFactory scopeFactory,
            IOptions<KafkaOptions> options,
            ILogger<UserPromotedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _options.Value.BootstrapServers,
                GroupId = _options.Value.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(_options.Value.ConsumerTopic);

            _logger.LogInformation("Kafka consumer started on topic: {Topic}", _options.Value.ConsumerTopic);

            await Task.Yield();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    var @event = JsonSerializer.Deserialize<UserPromotedToClubAdminEvent>(result.Message.Value);

                    if (@event is null) continue;

                    using var scope = _scopeFactory.CreateScope();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                    var user = await userManager.FindByEmailAsync(@event.Email);
                    if (user is not null)
                    {
                        var currentRoles = await userManager.GetRolesAsync(user);
                        if (!currentRoles.Contains(AppRoles.ClubAdmin))
                        {
                            await userManager.AddToRoleAsync(user, AppRoles.ClubAdmin);
                            _logger.LogInformation("Promoted {Email} to ClubAdmin", @event.Email);
                        }
                    }

                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                }
            }
        }
    }
}
