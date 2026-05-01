using GestionClubs.Application.BaseExceptions;

namespace GestionClubs.Application.Exceptions
{
    [Serializable]
    internal class UserAdhesionExistsException : AppConflictException
    {
        public UserAdhesionExistsException()
        {
        }

        public UserAdhesionExistsException(string? message) : base(message)
        {
        }

        public UserAdhesionExistsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}