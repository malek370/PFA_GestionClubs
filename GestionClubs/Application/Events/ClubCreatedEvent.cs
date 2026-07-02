namespace GestionClubs.Application.Events
{
    public class ClubCreatedEvent
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PresidentEmail { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
