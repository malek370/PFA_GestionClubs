namespace GestionClubs.Infrastructure.Kafka
{
    public class KafkaOptions
    {
        public const string SectionName = "Kafka";
        public string BootstrapServers { get; set; } = string.Empty;
        public string ProducerTopic { get; set; } = string.Empty;
        public string ConsumerTopic { get; set; } = string.Empty;
        public string ConsumerGroupId { get; set; } = string.Empty;
        public string ProducerTopicMember { get; set; } = string.Empty;
    }
}
