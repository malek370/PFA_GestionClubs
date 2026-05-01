using GestionClubs.Application.BaseExceptions;

namespace GestionClubs.Application.Exceptions
{
    [Serializable]
    internal class ClubExistsException : AppConflictException
    {
        public ClubExistsException()
        {
        }

        public ClubExistsException(string? message) : base(message)
        {
        }

        public ClubExistsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}