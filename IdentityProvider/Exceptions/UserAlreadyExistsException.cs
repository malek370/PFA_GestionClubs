namespace IdentityProvider.Exceptions
{
    public class UserAlreadyExistsException(string email) : Exception($"A user with the email '{email}' already exists.")
    {
    }
}
