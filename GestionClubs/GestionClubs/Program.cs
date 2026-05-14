using GestionClubs.API.Controllers;
using GestionClubs.API.Handlers;
using GestionClubs.Application.IServices;
using GestionClubs.Application.Services;
using GestionClubs.Domain.Entities;
using GestionClubs.Infrastructure.SqlServerDbContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//add database service with SQL Server provider and connection string from appsettings.json
builder.Services.AddInfrastructureServices_SqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

// Register application services
builder.Services.AddScoped<IClubServices, ClubServices>();
builder.Services.AddScoped<IMembersService, MembersService>();
builder.Services.AddScoped<IAdhesionService, AdhesionService>();
builder.Services.AddScoped<IAnnoucementService, AnnoucementService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddExceptionHandler<GlobalExcpectionHandler>();

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["IdentityProvider:Authority"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["IdentityProvider:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["IdentityProvider:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine($"Access Token received: {accessToken}");
                    Console.WriteLine("Authority: " + context.Options.Authority);
                    Console.WriteLine("Audience: " + context.Options.Audience);
                    Console.WriteLine("Issuer: " + context.Options.TokenValidationParameters.ValidIssuer);
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var claims = context.Principal?.Claims;
                var email = claims?.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
                var roles = claims?.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);

                logger.LogInformation("JWT token validated successfully for {Email} with roles: {Roles}",
                     email, string.Join(", ", roles ?? Enumerable.Empty<string>()));

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppRoles.PlatformAdmin, policy => policy.RequireRole(AppRoles.PlatformAdmin))
    .AddPolicy(AppRoles.ClubAdmin, policy => policy.RequireRole(AppRoles.ClubAdmin))
    .AddPolicy(AppRoles.ClubMember, policy => policy.RequireRole([AppRoles.ClubMember, AppRoles.ClubAdmin]))
    .AddPolicy(AppRoles.Visitor, policy => policy.RequireRole([AppRoles.Visitor, AppRoles.ClubAdmin, AppRoles.ClubMember]));
                                


var app = builder.Build();
// Apply migrations only if not using in-memory database (e.g., not in tests)


    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsSqlServer())
    {
        dbContext.Database.Migrate(); // Applies all pending migrations
    }

    }


// Seed the database
//await app.Services.SeedDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Title = "Documentation of GestionClubs";
    });
}
app.UseExceptionHandler(_ => { });
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

#region endpoints

// --- Clubs ---
app.AddClubEndpoints();

// --- Members ---
app.AddMembersEndpoints();


// --- Adhesions ---
app.AddAdhesionEndpoints();

//---tests ---
app.MapGet("/testAuth", () => "Hello World!").RequireAuthorization();
app.MapGet("/testPlatformAdmin", () => "hi admin").RequireAuthorization(AppRoles.PlatformAdmin);
app.MapGet("/testClubAdmin", () => "hi Club admin").RequireAuthorization(AppRoles.ClubAdmin);
app.MapGet("/testClubMember", () => "hi Club member").RequireAuthorization(AppRoles.ClubMember);

// --- Annoucements ---
app.AddAnnoucementEndpoints();


// --- Events ---
app.AddEventEndpoints();

#endregion




app.Run();

public partial class Program { }


