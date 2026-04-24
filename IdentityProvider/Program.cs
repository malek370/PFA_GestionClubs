using IdentityProvider.Abstracts;
using IdentityProvider.Controllers;
using IdentityProvider.DbContext;
using IdentityProvider.Entities;
using IdentityProvider.Handlers;
using IdentityProvider.Options;
using IdentityProvider.Processors;
using IdentityProvider.Repositories;
using IdentityProvider.Requests;
using IdentityProvider.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Cryptography;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

//seeding db 
builder.Services.AddScoped<SeedDb>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthTokenProcessor, AuthTokenProcessorAssymetricKey>();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));

//dbcontext
builder.Services.AddDbContext<IdpDbContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

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
            context.Token = context.Request.Cookies["ACCESS_TOKEN"];
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
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Applies all pending migrations
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