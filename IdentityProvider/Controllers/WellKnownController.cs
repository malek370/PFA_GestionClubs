using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace IdentityProvider.Controllers
{
    public static class WellKnownController
    {
        public static void WellKnownEndpoints(this WebApplication app)
        {
            app.MapGet("/.well-known/openid-configuration", (HttpContext httpContext) =>
            {
                var request = httpContext.Request;
                var baseUrl = $"{request.Scheme}://{request.Host}";
                return Results.Ok(new
                {
                    issuer = app.Configuration["JwtOptions:Issuer"],
                    jwks_uri = $"{baseUrl}/.well-known/jwks",
                    token_endpoint = $"{baseUrl}/connect/token",
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
