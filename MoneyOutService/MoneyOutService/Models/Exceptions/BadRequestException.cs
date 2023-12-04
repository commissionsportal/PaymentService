using System;

namespace MoneyOutService.Models.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string content) : base()
        {
            Content = content;
        }

        public string Content { get; set; }
    }
}
