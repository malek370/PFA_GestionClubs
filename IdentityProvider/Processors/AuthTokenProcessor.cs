using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace IdentityProvider.Processors
{
    public class AuthTokenProcessor : IAuthTokenProcessor
    {
        protected readonly JwtOptions _jwtOptions;
        protected readonly IHttpContextAccessor _httpContext;
        protected readonly UserManager<User> _userManager;
        protected readonly ILogger<AuthTokenProcessor> _logger;

        public AuthTokenProcessor(IOptions<JwtOptions> options, IHttpContextAccessor httpContext, UserManager<User> userManager, ILogger<AuthTokenProcessor> logger)
        {
            _jwtOptions = options.Value;
            _userManager = userManager;
            _httpContext = httpContext;
            _logger = logger;
        }
        public virtual async Task<(string token, DateTime expires)> GenerateToken(User user)
        {
            _logger.LogInformation("Generating symmetric JWT token for user {UserId} ({UserEmail})", user.Id, user.Email);

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtOptions.Secret));
            var (token,expires) = await PrepareCTokenClaims(key, user, SecurityAlgorithms.HmacSha256);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogDebug("JWT token generated successfully for user {UserId}. Expires at {ExpirationTime}", user.Id, expires);

            return (tokenString, expires);
        }
        public (string token, DateTime expires) GenerateRefreshToken()
        {
            _logger.LogInformation("Generating refresh token");

            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var token = Convert.ToBase64String(randomNumber);
            var expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);

            _logger.LogDebug("Refresh token generated successfully. Expires at {ExpirationTime}", expires);

            return (token, expires);
        }
        public void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expires)
        {
            _logger.LogInformation("Writing auth token as HTTP-only cookie: {CookieName}", cookieName);

            _httpContext.HttpContext!.Response.Cookies.Append(cookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Expires = expires,
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });

            _logger.LogDebug("Cookie {CookieName} written successfully. Expires at {ExpirationTime}", cookieName, expires);
        }
        public async Task<(JwtSecurityToken,DateTime)> PrepareCTokenClaims(SecurityKey key,User user,string securityAlgorithm)
        {
            _logger.LogDebug("Preparing JWT token claims for user {UserId} with algorithm {Algorithm}", user.Id, securityAlgorithm);

            var creds = new SigningCredentials(key, securityAlgorithm);
            var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.ToString())
            };
            var UserRoles = await _userManager.GetRolesAsync(user);

            _logger.LogDebug("Adding {RoleCount} roles to token claims for user {UserId}: {Roles}", 
                UserRoles.Count, user.Id, string.Join(", ", UserRoles));

            foreach (var role in UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            _logger.LogDebug("JWT token claims prepared successfully for user {UserId}. Token expires at {ExpirationTime}", 
                user.Id, expires);

            return (token,expires);
        }
    }
}
