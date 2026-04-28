using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Exceptions;
using IdentityProvider.Requests;
using IdentityProvider.Responses;
using Microsoft.AspNetCore.Identity;

namespace IdentityProvider.Services
{
    public class AccountService : IAccountService
    {
        public readonly UserManager<User> _userManager;
        public readonly IAuthTokenProcessor _authTokenProcessor;
        public readonly IUserRepository _userRepository;
        public AccountService(UserManager<User> userManager, IAuthTokenProcessor authTokenProcessor, IUserRepository userRepository)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _authTokenProcessor = authTokenProcessor;

        }
        public async Task<TokenResponse> LoginAsync(LoginRequest loginRequest)
        {
            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginRequest.Password)) throw new LoginFailedException(loginRequest.Email);

           return await GenerateAndStoreTokensAsync(user);

        }
        public async Task RegisterAsync(RegisterRequest registerRequest)
        {
            if (_userManager.Users.Any(u => u.Email == registerRequest.Email)) throw new UserAlreadyExistsException(registerRequest.Email);
            var user = User.Create(registerRequest.Email, registerRequest.FirstName, registerRequest.LastName);
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, registerRequest.Password);
            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                throw new RegistrationFailedException(result.Errors.Select(e => e.Description));
            }
            var resultRole = await _userManager.AddToRoleAsync(user, AppRoles.Visitor);
                if (!resultRole.Succeeded)
                {
                    throw new RegistrationFailedException(resultRole.Errors.Select(e => e.Description));
            }

        }
        public async Task<TokenResponse> RefreshTokenAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken)) throw new RefreshTokenException("Refresh token is missing");


            var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow) throw new RefreshTokenException("Invalid refresh token");
            return await GenerateAndStoreTokensAsync(user);
            //repeated code


        }
        private async Task<TokenResponse> GenerateAndStoreTokensAsync(User user)
        {
            var (accessToken, expirationDate) =await _authTokenProcessor.GenerateToken(user);
            var (refreshToken_new, refreshTokenExpiration) = _authTokenProcessor.GenerateRefreshToken();
            user.RefreshToken = refreshToken_new;
            user.RefreshTokenExpiryTime = refreshTokenExpiration;
            await _userManager.UpdateAsync(user);
            _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", accessToken, expirationDate);
            _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, user.RefreshTokenExpiryTime);
            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = user.RefreshToken,
                RefreshTokenExpires = user.RefreshTokenExpiryTime,
                AccessTokenExpires = expirationDate
            };
        }
    }
}
