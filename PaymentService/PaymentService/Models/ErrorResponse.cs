namespace PaymentService.Models
{
    public class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public InnerErrorResponse InnerException { get; set; } = new InnerErrorResponse();
    }

    public class InnerErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
    }
}
