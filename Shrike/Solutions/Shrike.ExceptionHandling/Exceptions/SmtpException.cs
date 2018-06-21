using System;

namespace Shrike.ExceptionHandling.Exceptions
{
    [Serializable]
    public class SmtpException : ApplicationException
    {
        private const string MessageFormat = "SMTP Error: {0}";
        private readonly string _mesageDetails = String.Empty;

        public SmtpException()
        {
        }

        public SmtpException(string message)
            : base(message)
        {
            _mesageDetails = message;
        }

        public SmtpException(string message, Exception innerException)
            : base(message, innerException)
        {
            _mesageDetails = message;
        }

        protected SmtpException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }

        public override string Message
        {
            get { return string.Format(MessageFormat, _mesageDetails); }
        }

    }
}