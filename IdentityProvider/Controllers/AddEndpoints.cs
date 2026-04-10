using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Requests;

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
        }
    }
}
