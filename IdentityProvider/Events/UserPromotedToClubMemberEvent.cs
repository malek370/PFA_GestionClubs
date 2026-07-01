namespace IdentityProvider.Events
{
    public record UserPromotedToClubMemberEvent
    {
        public string Email { get; init; } = string.Empty;
        public int ClubId { get; init; }
        public DateTime PromotedAt { get; init; }
    }
}
