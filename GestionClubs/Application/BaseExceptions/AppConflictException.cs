using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestionClubs.Application.BaseExceptions
{
    public class AppConflictException:Exception
    {
        public AppConflictException()
        {
        }

        public AppConflictException(string? message) : base(message)
        {
        }

        public AppConflictException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
