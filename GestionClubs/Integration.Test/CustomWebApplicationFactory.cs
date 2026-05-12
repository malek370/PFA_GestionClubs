using GestionClubs.Application.IServices;
using GestionClubs.Domain.Entities;
using GestionClubs.Infrastructure.SqlServerDbContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Test;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();
    public string CurrentUserEmail { get; set; } = "test@example.com";
    public List<string> CurrentUserRoles { get; set; } = [AppRoles.Visitor];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core and DbContext-related service registrations to avoid provider conflicts
            var toRemove = services
                .Where(d => d.ServiceType == typeof(AppDbContext)
                         || d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.Namespace?.Contains("EntityFrameworkCore") == true)
                         || (d.ImplementationType?.Assembly?.GetName()?.Name?.Contains("SqlServer") == true)
                         || (d.ImplementationInstance?.GetType().Assembly?.GetName()?.Name?.Contains("SqlServer") == true)
                         || (d.ImplementationFactory?.Method.DeclaringType?.Assembly?.GetName()?.Name?.Contains("SqlServer") == true))
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            // Add InMemory provider
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Replace authentication with a test scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Replace ICurrentUserService with a fake
            var currentUserDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICurrentUserService));
            if (currentUserDescriptor != null) services.Remove(currentUserDescriptor);
            services.AddScoped<ICurrentUserService>(_ => new FakeCurrentUserService(CurrentUserEmail));
        });

        builder.UseEnvironment("Development");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Seed a test user matching the email used in FakeCurrentUserService
        if (!db.Users.Any(u => u.Email == "test@example.com"))
        {
            db.Users.Add(new User { Id = 1, Email = "test@example.com", FirstName = "Test", LastName = "User" });
            db.Users.Add(new User { Id = 2, Email = "club@test.com", FirstName = "clubTest", LastName = "clubUser" });
        }

        // Seed a club with Id = 1
        if (!db.Clubs.Any(c => c.Id == 1))
        {
            db.Clubs.Add(new Club { Id = 1, Name = "Test Club", Description = "A test club" });
        }

        db.SaveChanges();

        return host;
    }

    public HttpClient CreateClientWithRole(params string[] roles)
    {
        CurrentUserRoles = [.. roles];
        return CreateClient();
    }
}

public class FakeCurrentUserService(string email) : ICurrentUserService
{
    public string? GetEmail() => email;
    public Task CheckUserIsAdminForClub(int clubId) => Task.CompletedTask;
}

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read roles from request header set by tests
        var rolesHeader = Request.Headers["X-Test-Roles"].FirstOrDefault() ?? AppRoles.Visitor;

        if (rolesHeader == "None")
            return Task.FromResult(AuthenticateResult.Fail("No authentication provided."));

        var roles = rolesHeader.Split(',');

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "testuser"),
            new(ClaimTypes.Email, "test@example.com"),
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
