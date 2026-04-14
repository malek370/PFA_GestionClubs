using IdentityProvider.Entities;

namespace IdentityProvider.Options
{
    public static class AuthorizationOptions
    {
        public static void AddPolicies(this WebApplicationBuilder builder)
        {

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(AppRoles.ClubAdmin, policy => policy.RequireRole(AppRoles.ClubAdmin));
                options.AddPolicy(AppRoles.ClubMember, policy => policy.RequireRole(AppRoles.ClubMember));
                options.AddPolicy(AppRoles.PlatformAdmin, policy => policy.RequireRole(AppRoles.PlatformAdmin));
                options.AddPolicy(AppRoles.Chatbot, policy => policy.RequireRole(AppRoles.Chatbot));
            });
        }
    }
}
