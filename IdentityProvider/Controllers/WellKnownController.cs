using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace IdentityProvider.Controllers
{
    public static class WellKnownController
    {
        public static void WellKnownEndpoints(this WebApplication app)
        {
            app.MapGet("/.well-known/openid-configuration", async () =>
            {
                var issuer = $"{app.Urls.FirstOrDefault()}/";
                return Results.Ok(new
                {
                    issuer,
                    jwks_uri = $"{issuer}.well-known/jwks",
                    token_endpoint = $"{issuer}connect/token",
                });
            }
);
            app.MapGet("/.well-known/jwks", () =>
            {
                var publicKey = File.ReadAllText("public_key.pem");
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKey);
                var parameters = rsa.ExportParameters(false);
                return Results.Ok(new
                {
                    keys = new[]
                    {
            new
            {
                kty = "RSA",
                use = "sig",
                kid = "1",
                e = Base64UrlEncoder.Encode(parameters.Exponent),
                n = Base64UrlEncoder.Encode(parameters.Modulus)
            }
        }
                });
            }
            );
        }
    }
}
