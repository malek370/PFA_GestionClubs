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
    public class AuthTokenProcessorAssymetricKey : AuthTokenProcessor
    {
        public AuthTokenProcessorAssymetricKey(IOptions<JwtOptions> options, IHttpContextAccessor httpContext, UserManager<User> userManager, ILogger<AuthTokenProcessorAssymetricKey> logger)
            : base(options, httpContext, userManager, logger)
        {
        }

        public override async Task<(string token, DateTime expires)> GenerateToken(User user)
        {
            _logger.LogInformation("Generating asymmetric JWT token for user {UserId} ({UserEmail})", user.Id, user.Email);

            var rsa = RSA.Create();
            var file = await File.ReadAllTextAsync("private_key.pem");
            rsa.ImportFromPem(file);
            var privateKey = new RsaSecurityKey(rsa) { KeyId = "1" };

            _logger.LogDebug("RSA private key loaded successfully for token generation");

            var (token, expires) = await PrepareCTokenClaims(privateKey, user, SecurityAlgorithms.RsaSha256);
            var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogDebug("Asymmetric JWT token generated successfully for user {UserId}. Expires at {ExpirationTime}", user.Id, expires);

            return (tokenString, expires);
        }
    }
}
