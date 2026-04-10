using IdentityProvider.Abstracts;
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
using Scalar.AspNetCore;
using System.Threading.Tasks;

namespace IdentityProvider
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            //seeding db 
            builder.Services.AddScoped<SeedDb>();


            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IAuthTokenProcessor, AuthTokenProcessor>();
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
                .AddEntityFrameworkStores <IdpDbContext>()
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
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOptions.Secret)),
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
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("MemberOrVisitor", policy =>
                    policy.RequireRole(AppRoles.ClubMember, AppRoles.Visitor));
                options.AddPolicy(AppRoles.ClubAdmin, policy =>
                    policy.RequireRole(AppRoles.ClubAdmin));
                options.AddPolicy(AppRoles.Visitor, policy =>
                    policy.RequireRole(AppRoles.Visitor));
                options.AddPolicy(AppRoles.ClubMember, policy =>
                    policy.RequireRole(AppRoles.ClubMember));
                options.AddPolicy(AppRoles.PlatformAdmin, policy =>
                    policy.RequireRole(AppRoles.PlatformAdmin));
                options.AddPolicy(AppRoles.Chatbot, policy =>
                    policy.RequireRole(AppRoles.Chatbot));
            });

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

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


            app.MapPost("/api/account/register", async (RegisterRequest request, IAccountService accountService) =>
            {
                await accountService.RegisterAsync(request);
                return Results.Ok();
            });
            app.MapPost("/api/account/login", async (LoginRequest request, IAccountService accountService) =>
            {
                await accountService.LoginAsync(request);
                return Results.Ok();
            });
            app.MapPost("/api/account/refresh-token", async (HttpContext http, IAccountService accountService) =>
            {
                await accountService.RefreshTokenAsync(http.Request.Cookies["REFRESH_TOKEN"]);
                return Results.Ok();
            });
            app.MapGet("/api/account/protected", () => "This is a protected endpoint").RequireAuthorization();
            app.MapGet("/api/account/member", () => "This is a protected endpoint for member").RequireAuthorization(AppRoles.ClubMember);
            app.MapGet("/api/account/platformadmin", () => "This is a protected endpoint for PLATFORM ADMIN").RequireAuthorization(AppRoles.PlatformAdmin);


            app.Run();
        }
    }
}
