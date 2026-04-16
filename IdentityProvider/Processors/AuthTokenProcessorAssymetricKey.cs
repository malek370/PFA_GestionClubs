using IdentityProvider.Entities;
using IdentityProvider.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
namespace IdentityProvider.Processors
{
    public class AuthTokenProcessorAssymetricKey(IOptions<JwtOptions> options, IHttpContextAccessor httpContext, UserManager<User> userManager)
        : AuthTokenProcessor(options, httpContext, userManager)
    {
        public override async Task<(string token, DateTime expires)> GenerateToken(User user)
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText("private_key.pem"));
            var privateKey = new RsaSecurityKey(rsa) { KeyId = "1" };

            var creds = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
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
    }
}
