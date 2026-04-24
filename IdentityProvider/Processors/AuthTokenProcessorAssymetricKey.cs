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
            var file = await File.ReadAllTextAsync("private_key.pem");
            rsa.ImportFromPem(file);
            var privateKey = new RsaSecurityKey(rsa) { KeyId = "1" };

            var (token, expires) = await PrepareCTokenClaims(privateKey, user, SecurityAlgorithms.RsaSha256);
            return (new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
