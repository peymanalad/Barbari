using System;

namespace BarcopoloWebApi.Exceptions 
{
    public class BadRequestException : Exception
    {

        public BadRequestException()
            : base("درخواست نامعتبر است.") // یک پیام پیش‌فرض
        {
        }
        public BadRequestException(string message)
            : base(message)
        {
        }


        public BadRequestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }
}