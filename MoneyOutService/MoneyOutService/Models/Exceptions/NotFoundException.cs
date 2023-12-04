using System;

namespace MoneyOutService.Models.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string content) : base()
        {
            Content = content;
        }

        public string Content { get; set; }
    }
}
