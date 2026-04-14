using IdentityProvider.Entities;

namespace IdentityProvider.Abstracts
{
    public interface IUserRepository
    {
        public  Task<User?> GetUserByRefreshTokenAsync(string token);
    }
}
