namespace IdentityProvider.Options
{
    public class JwtOptions
    {
        public const string JwtOptionsKey = "JwtOptions";
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; }
    }
}
