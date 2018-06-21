using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shrike.ExceptionHandling.Exceptions
{
    public class DataAccessException : Exception
    {
        public DataAccessException()
        {
        }

        public DataAccessException(string message)
            : base(message)
        {
        }

        public DataAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
