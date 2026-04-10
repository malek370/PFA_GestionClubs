using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;

namespace IdentityProvider.Processors
{
    public class AuthTokenProcessor: IAuthTokenProcessor
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IHttpContextAccessor _httpContext;
        public AuthTokenProcessor(IOptions<JwtOptions> options,IHttpContextAccessor httpContext)
        {
            _jwtOptions = options.Value;
            _httpContext = httpContext;

        }
        public (string token, DateTime expires) GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.ToString())
            };
            var expiers = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            return (new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token), expires);
        }
        public (string token, DateTime expires) GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return (Convert.ToBase64String(randomNumber),DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays));
        }
        public void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token,DateTime expires)
        {
            _httpContext.HttpContext.Response.Cookies.Append(cookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Expires = expires,
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }

    }
}
