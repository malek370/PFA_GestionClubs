namespace IdentityProvider.Exceptions
{
    public class LoginFailedException(string email) : Exception($"Email {email} does not exists or wrong password")
    {
    }
}
