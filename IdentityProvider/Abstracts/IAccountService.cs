using IdentityProvider.Requests;
using IdentityProvider.Responses;

namespace IdentityProvider.Abstracts
{
    public interface IAccountService
    {
        Task<TokenResponse> LoginAsync(LoginRequest loginRequest);
        Task RegisterAsync(RegisterRequest registerRequest);
        Task<TokenResponse> RefreshTokenAsync(string? refreshToken);
    }
}