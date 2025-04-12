namespace BarcopoloWebApi.Exceptions
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException() : base("دسترسی غیرمجاز") { }

        public ForbiddenAccessException(string message) : base(message) { }
    }
}