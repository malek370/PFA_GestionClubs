using GestionClubs.Application.BaseExceptions;

namespace GestionClubs.Application.Exceptions
{
    [Serializable]
    internal class UserAlreadyMemberException : AppConflictException
    {
        public UserAlreadyMemberException()
        {
        }

        public UserAlreadyMemberException(string? message) : base(message)
        {
        }

        public UserAlreadyMemberException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}