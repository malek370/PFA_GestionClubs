using Microsoft.AspNetCore.Identity;

namespace IdentityProvider.Entities
{
    public class User : IdentityUser<Guid>
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public static User Create(string email, string firstName, string lastName)
        {
            return new User
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };
        }
        public override string ToString()
        {
            return $"{FirstName} {LastName} ({Email})";

        }
    }
}
