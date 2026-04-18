using IdentityProvider.Abstracts;
using IdentityProvider.DbContext;
using IdentityProvider.Processors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.IO;
using System.Security.Cryptography;

namespace IdentityProvider.IdentityProviderTests.Endpoints;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IAccountService> AccountServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Générer une paire RSA temporaire pour les tests
        var rsa = RSA.Create(2048);
        var publicKeyPem = rsa.ExportRSAPublicKeyPem();
        var privateKeyPem = rsa.ExportRSAPrivateKeyPem();

        // Écrire les fichiers PEM dans le répertoire de travail courant (bin/Debug/net9.0)
        // C'est là que Program.cs les cherche via File.ReadAllText("public_key.pem")
        var binDir = Directory.GetCurrentDirectory();
        File.WriteAllText(Path.Combine(binDir, "public_key.pem"), publicKeyPem);
        File.WriteAllText(Path.Combine(binDir, "private_key.pem"), privateKeyPem);

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtOptions:Secret"] = "TestSecret1234567890123456789012345678901234567890",
                ["JwtOptions:Issuer"] = "test-issuer",
                ["JwtOptions:Audience"] = "test-audience",
                ["JwtOptions:ExpirationMinutes"] = "15",
                ["JwtOptions:RefreshTokenExpirationDays"] = "7",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remplacer le DbContext SqlServer par InMemory
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<IdpDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<IdpDbContext>(opt =>
                opt.UseInMemoryDatabase("TestDb"));

            // Remplacer IAccountService par le mock
            var accountServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IAccountService));
            if (accountServiceDescriptor != null) services.Remove(accountServiceDescriptor);

            services.AddScoped<IAccountService>(_ => AccountServiceMock.Object);

            // Supprimer le seeder pour éviter les erreurs
            var seedDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(SeedDb));
            if (seedDescriptor != null) services.Remove(seedDescriptor);
        });

        builder.UseEnvironment("Production");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Nettoyer les fichiers PEM temporaires
        var binDir = Directory.GetCurrentDirectory();

        var publicPem = Path.Combine(binDir, "public_key.pem");
        var privatePem = Path.Combine(binDir, "private_key.pem");

        if (File.Exists(publicPem)) File.Delete(publicPem);
        if (File.Exists(privatePem)) File.Delete(privatePem);
    }
}