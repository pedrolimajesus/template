using System;

namespace Shrike.ExceptionHandling.Exceptions
{
    public class UILogicException : Exception
    {
        public UILogicException()
        {
        }

        public UILogicException(string message)
            : base(message)
        {
        }

        public UILogicException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}