using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Application.BaseExceptions
{
    public class AppUnauthorizedException : Exception
    {
        public AppUnauthorizedException()
        {
        }

        public AppUnauthorizedException(string? message) : base(message)
        {
        }

        public AppUnauthorizedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
