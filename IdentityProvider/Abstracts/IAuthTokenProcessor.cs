using IdentityProvider.Entities;

namespace IdentityProvider.Abstracts
{
    public interface IAuthTokenProcessor
    {
        Task<(string token, DateTime expires)> GenerateToken(User user);
        (string token, DateTime expires) GenerateRefreshToken();
        void WriteAuthTokenAsHttpOnlyCookie(string cookieName, string token, DateTime expires);
    }
}
