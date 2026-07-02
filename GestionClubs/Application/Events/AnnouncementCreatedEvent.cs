namespace GestionClubs.Application.Events
{
    public class AnnouncementCreatedEvent
    {
        public int AnnouncementId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ClubName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
