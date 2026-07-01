using Confluent.Kafka;
using IdentityProvider.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IdentityProvider.Kafka
{
    public class KafkaProducer : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducer> _logger;

        public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
        {
            _logger = logger;
            var config = new ProducerConfig
            {
                BootstrapServers = options.Value.BootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 3
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string> { Key = key, Value = json };

            var result = await _producer.ProduceAsync(topic, kafkaMessage, ct);
            _logger.LogInformation("Published to {Topic} [{Partition}] @ offset {Offset}",
                result.Topic, result.Partition.Value, result.Offset.Value);
        }

        public void Dispose() => _producer?.Dispose();
    }
}
