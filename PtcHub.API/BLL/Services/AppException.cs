namespace PtcHub.API.BLL.Services
{
    // Business logic exception — carries a message + HTTP status code
    public class AppException : Exception
    {
        public int StatusCode { get; }

        public AppException(string message, int statusCode = 400) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}