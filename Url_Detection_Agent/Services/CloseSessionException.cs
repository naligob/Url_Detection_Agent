namespace Url_Detection_Agent.Services
{
    public class CloseSessionException : Exception
    {
        public CloseSessionException() : base() { }

        public CloseSessionException(string message) : base(message) { }

        public CloseSessionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
