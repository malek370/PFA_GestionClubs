using IdentityProvider.Abstracts;
using IdentityProvider.Entities;
using IdentityProvider.Exceptions;
using IdentityProvider.Requests;
using Microsoft.AspNetCore.Identity;

namespace IdentityProvider.Services
{
    public class AccountService : IAccountService
    {
        public readonly UserManager<User> _userManager;
        public readonly IAuthTokenProcessor _authTokenProcessor;
        public readonly IUserRepository _userRepository;
        private readonly ILogger<AccountService> _logger;

        public AccountService(UserManager<User> userManager, IAuthTokenProcessor authTokenProcessor, IUserRepository userRepository, ILogger<AccountService> logger)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _authTokenProcessor = authTokenProcessor;
            _logger = logger;
        }
        public async Task LoginAsync(LoginRequest loginRequest)
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginRequest.Email);

            var user = await _userManager.FindByEmailAsync(loginRequest.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for email {Email}", loginRequest.Email);
                throw new LoginFailedException(loginRequest.Email);
            }

            if (!await _userManager.CheckPasswordAsync(user, loginRequest.Password))
            {
                _logger.LogWarning("Login failed: Invalid password for user {UserId} ({Email})", user.Id, loginRequest.Email);
                throw new LoginFailedException(loginRequest.Email);
            }

            _logger.LogInformation("Login successful for user {UserId} ({Email})", user.Id, loginRequest.Email);
            await GenerateAndStoreTokensAsync(user);
        }
        public async Task RegisterAsync(RegisterRequest registerRequest)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", registerRequest.Email);

            if (_userManager.Users.Any(u => u.Email == registerRequest.Email))
            {
                _logger.LogWarning("Registration failed: User already exists for email {Email}", registerRequest.Email);
                throw new UserAlreadyExistsException(registerRequest.Email);
            }

            var user = User.Create(registerRequest.Email, registerRequest.FirstName, registerRequest.LastName);
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, registerRequest.Password);
            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Registration failed for email {Email}: {Errors}", 
                    registerRequest.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new RegistrationFailedException(result.Errors.Select(e => e.Description));
            }

            var resultRole = await _userManager.AddToRoleAsync(user, AppRoles.Visitor);
            if (!resultRole.Succeeded)
            {
                _logger.LogError("Failed to assign Visitor role to user {UserId} ({Email}): {Errors}", 
                    user.Id, registerRequest.Email, string.Join(", ", resultRole.Errors.Select(e => e.Description)));
                throw new RegistrationFailedException(resultRole.Errors.Select(e => e.Description));
            }

            _logger.LogInformation("Registration successful for user {UserId} ({Email})", user.Id, registerRequest.Email);
        }
        public async Task RefreshTokenAsync(string? refreshToken)
        {
            _logger.LogInformation("Refresh token attempt");

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Refresh token failed: Token is missing");
                throw new RefreshTokenException("Refresh token is missing");
            }

            var user = await _userRepository.GetUserByRefreshTokenAsync(refreshToken);
            if (user == null)
            {
                _logger.LogWarning("Refresh token failed: User not found for provided refresh token");
                throw new RefreshTokenException("Invalid refresh token");
            }

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token failed: Token expired for user {UserId} ({Email})", user.Id, user.Email);
                throw new RefreshTokenException("Invalid refresh token");
            }

            _logger.LogInformation("Refresh token successful for user {UserId} ({Email})", user.Id, user.Email);
            await GenerateAndStoreTokensAsync(user);
        }
        private async Task GenerateAndStoreTokensAsync(User user)
        {
            _logger.LogDebug("Generating and storing tokens for user {UserId} ({Email})", user.Id, user.Email);

            var (accessToken, expirationDate) = await _authTokenProcessor.GenerateToken(user);
            var (refreshToken_new, refreshTokenExpiration) = _authTokenProcessor.GenerateRefreshToken();

            user.RefreshToken = refreshToken_new;
            user.RefreshTokenExpiryTime = refreshTokenExpiration;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to update user {UserId} with new refresh token: {Errors}", 
                    user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }

            _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("ACCESS_TOKEN", accessToken, expirationDate);
            _authTokenProcessor.WriteAuthTokenAsHttpOnlyCookie("REFRESH_TOKEN", user.RefreshToken, user.RefreshTokenExpiryTime);

            _logger.LogDebug("Tokens generated and stored successfully for user {UserId}", user.Id);
        }
    }
}
