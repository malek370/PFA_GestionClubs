using Confluent.Kafka;
using GestionClubs.Application.Events;
using GestionClubs.Domain.Entities;
using GestionClubs.Infrastructure.Kafka;
using GestionClubs.Infrastructure.SqlServerDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GestionClubs.Infrastructure.Consumers
{
    public class UserRegisteredConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<KafkaOptions> _options;
        private readonly ILogger<UserRegisteredConsumer> _logger;

        public UserRegisteredConsumer(
            IServiceScopeFactory scopeFactory,
            IOptions<KafkaOptions> options,
            ILogger<UserRegisteredConsumer> logger)
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
                    var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(result.Message.Value);

                    if (@event is null) continue;

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var exists = await dbContext.Users.AnyAsync(u => u.Email == @event.Email, stoppingToken);
                    if (!exists)
                    {
                        dbContext.Users.Add(new User
                        {
                            Email = @event.Email,
                            FirstName = @event.FirstName,
                            LastName = @event.LastName
                        });
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation("Created user {Email} from Kafka event", @event.Email);
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
