namespace GestionClubs.Application.Events
{
    public class EventCreatedEvent
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
