using IdentityProvider.Requests;

namespace IdentityProvider.Abstracts
{
    public interface IAccountService
    {
        Task LoginAsync(LoginRequest loginRequest);
        Task RegisterAsync(RegisterRequest registerRequest);
        Task RefreshTokenAsync(string? refreshToken);
    }
}