namespace IdentityProvider.Exceptions
{
    public class RegistrationFailedException(IEnumerable<string> errors) : Exception($"Registration failed: {string.Join(Environment.NewLine, errors)}")
    {
    }
}
