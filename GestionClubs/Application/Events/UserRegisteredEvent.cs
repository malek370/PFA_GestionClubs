namespace GestionClubs.Application.Events
{
    public record UserRegisteredEvent
    {
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public DateTime RegisteredAt { get; init; }
    }
}
