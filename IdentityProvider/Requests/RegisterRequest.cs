using System.ComponentModel.DataAnnotations;

namespace IdentityProvider.Requests
{
    public class RegisterRequest
    {
        [EmailAddress]
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Password { get; set; }
        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public required string ConfirmPassword { get; set; }
        [Required]
        public required string FirstName { get; set; }
        [Required]
        public required string LastName { get; set; }
    }
}
