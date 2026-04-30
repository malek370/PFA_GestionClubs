using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Requests;
using IdentityProvider.Validators;

namespace IdentityProvider.Controllers
{
    public static class AddEndpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            app.MapPost("/api/account/register", async (RegisterRequest request, IAccountService accountService) =>
            {
                await accountService.RegisterAsync(request);
                return Results.Ok();
            }).AddEndpointFilter<ValidationFilter<RegisterRequest>>();

            app.MapPost("/api/account/login", async (LoginRequest request, IAccountService accountService) =>
            {
                var result = await accountService.LoginAsync(request);
                return Results.Ok(result);
            }).AddEndpointFilter<ValidationFilter<LoginRequest>>(); 

            app.MapPost("/api/account/refresh-token", async (HttpContext http, IAccountService accountService) =>
            {
                var result = await accountService.RefreshTokenAsync(http.Request.Headers["REFRESH_TOKEN"]);
                return Results.Ok(result);
            });

            app.MapGet("/api/account/protected", () => "This is a protected endpoint").RequireAuthorization();
            app.MapGet("/api/account/member", () => "This is a protected endpoint for member of a club").RequireAuthorization(AppRoles.ClubMember);
            app.MapGet("/api/account/platformadmin", () => "This is a protected endpoint for PLATFORM ADMIN ONLY.").RequireAuthorization(AppRoles.PlatformAdmin);
        }
    }
}
