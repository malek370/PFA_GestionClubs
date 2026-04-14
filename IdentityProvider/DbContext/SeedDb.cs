using IdentityProvider.Entities;
using IdentityProvider.Exceptions;
using IdentityProvider.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IdentityProvider.DbContext
{
    public class SeedDb
    {

        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        public SeedDb(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task SeedRoles()
        {
            bool rolesExist = await _roleManager.Roles.AnyAsync();
            if (!rolesExist)
            {
                var roles = new[] { AppRoles.ClubAdmin, AppRoles.ClubMember, AppRoles.PlatformAdmin, AppRoles.Visitor, AppRoles.Chatbot };
                foreach (var role in roles)
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }
        }
        public async Task SeedAdminUser()
        {
            if (await _userManager.Users.AnyAsync()) return;

            var seedData = await File.ReadAllTextAsync("DbContext/SeedData.json");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var users = JsonSerializer.Deserialize<List<SeedRequest>>(seedData, options);
            if(users == null || users.Count == 0)
            {
                throw new RegistrationFailedException(["No user data found in SeedData.json"]);
            }
            foreach (var user in users)
            {
                var newUser = User.Create(user.Email, user.FirstName, user.LastName);
                newUser.PasswordHash = _userManager.PasswordHasher.HashPassword(newUser, user.Password);

                var resultUser = await _userManager.CreateAsync(newUser);
                var resultRole = await _userManager.AddToRoleAsync(newUser, user.Role);
                if (!resultUser.Succeeded )
                {
                    throw new RegistrationFailedException(resultUser.Errors.Select(e => e.Description));
                }
                if (!resultRole.Succeeded)
                {
                    throw new RegistrationFailedException(resultRole.Errors.Select(e => e.Description));
                }
            }
        }
    }
}
