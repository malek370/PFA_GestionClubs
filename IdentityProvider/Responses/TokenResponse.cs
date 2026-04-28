namespace IdentityProvider.Responses
{
    public class TokenResponse
    {
        public required string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpires { get; set; }
        public DateTime AccessTokenExpires { get; set; }

    }
}
