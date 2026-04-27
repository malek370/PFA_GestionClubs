using IdentityProvider.Abstracts;
using IdentityProvider.Controllers;
using IdentityProvider.DbContext;
using IdentityProvider.Entities;
using IdentityProvider.Handlers;
using IdentityProvider.Middleware;
using IdentityProvider.Options;
using IdentityProvider.Processors;
using IdentityProvider.Repositories;
using IdentityProvider.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Cryptography;
var builder = WebApplication.CreateBuilder(args);

//seeding db 
builder.Services.AddScoped<SeedDb>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthTokenProcessor, AuthTokenProcessorAssymetricKey>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));

//dbcontext
//builder.Services.AddDbContext<IdpDbContext>(opt =>
//{
//    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
//});
builder.Services.AddDbContext<IdpDbContext>(options =>
    options.UseSqlite("Data Source=mabase.db"));

// AddIdentity AVANT AddAuthentication
builder.Services.AddIdentity<User, IdentityRole<Guid>>(opts =>
{
    opts.Password.RequireDigit = true;
    opts.Password.RequireLowercase = true;
    opts.Password.RequireUppercase = true;
    opts.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<IdpDbContext>()
    .AddRoles<IdentityRole<Guid>>()
    .AddRoleManager<RoleManager<IdentityRole<Guid>>>()
    .AddDefaultTokenProviders();

// Reconfigurer les schémas par défaut APRÈS AddIdentity
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.JwtOptionsKey)
        .Get<JwtOptions>() ?? throw new ArgumentException(nameof(JwtOptions));

    // Ne PAS utiliser options.Authority avec une valeur non-URL
    options.RequireHttpsMetadata = false;

    var rsa = RSA.Create();
    rsa.ImportFromPem(File.ReadAllText("public_key.pem"));
    var publicKey = new RsaSecurityKey(rsa) { KeyId = "1" };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = publicKey
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            var token = context.Request.Cookies["ACCESS_TOKEN"];
            if (!string.IsNullOrEmpty(token))
            {
                logger.LogDebug("JWT token received from ACCESS_TOKEN cookie for request {Path}", context.Request.Path);
                context.Token = token;
            }
            else
            {
                logger.LogDebug("No ACCESS_TOKEN cookie found for request {Path}", context.Request.Path);
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var claims = context.Principal?.Claims;
            var userId = claims?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            var email = claims?.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value;
            var roles = claims?.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);

            logger.LogInformation("JWT token validated successfully for user {UserId} ({Email}) with roles: {Roles}", 
                userId, email, string.Join(", ", roles ?? Enumerable.Empty<string>()));

            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            logger.LogWarning("JWT token validation failed for request {Path}: {Error}", 
                context.Request.Path, context.Exception.Message);

            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            logger.LogWarning("Access forbidden for request {Path}", context.Request.Path);

            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            logger.LogWarning("Authentication challenge triggered for request {Path}: {Error}", 
                context.Request.Path, context.ErrorDescription ?? "No token provided");

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.AddPolicies();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations only if not using in-memory database (e.g., not in tests)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdpDbContext>();
    try
    {
        dbContext.Database.Migrate(); // Applies all pending migrations
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("InMemory"))
    {
        // Skip migration for in-memory databases used in tests
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<SeedDb>();
        await seeder.SeedRoles();
        await seeder.SeedAdminUser();
    }
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Title = "Documentation of IdentityProvider";
    });
}

app.UseExceptionHandler(_ => { });

// Add request/response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();
app.WellKnownEndpoints();

await app.RunAsync();

// Nécessaire pour que WebApplicationFactory<Program> puisse référencer le type Program
namespace IdentityProvider
{
    public partial class Program { }
}