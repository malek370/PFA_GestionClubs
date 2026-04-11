using IdentityProvider.Abstracts;
using IdentityProvider.DbContext;
using IdentityProvider.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityProvider.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly IdpDbContext _dbContext;
        public UserRepository(IdpDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<User?> GetUserByRefreshTokenAsync(string token)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(user=>user.RefreshToken==token);
        }
    }
}
