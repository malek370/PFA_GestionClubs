using System.ComponentModel.DataAnnotations;

namespace IdentityProvider.Requests
{
    public class LoginRequest
    {
        [EmailAddress]
        public required string Email { get; set; }
        [Required]
        public required string Password { get; set; }
    }
}
