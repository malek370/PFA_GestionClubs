using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace IdentityProvider.Processors
{
    public class AuthTokenProcessor: IAuthTokenProcessor
    {
        private readonly JwtOptions _jwtOptions;
        private readonly IHttpContextAccessor _httpContext;
        private readonly UserManager<User> _userManager;
        public AuthTokenProcessor(IOptions<JwtOptions> options,IHttpContextAccessor httpContext, UserManager<User> userManager)
        {
            _jwtOptions = options.Value;
            _userManager = userManager;
            _httpContext = httpContext;

        }
        public async Task<(string token, DateTime expires)> GenerateToken(User user)
        {
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.ToString())
            };
            var UserRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

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
