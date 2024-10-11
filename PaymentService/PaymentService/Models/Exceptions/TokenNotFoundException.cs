namespace PaymentService.Models.Exceptions
{
    public class TokenNotFoundException : Exception
    {
        public TokenNotFoundException() : base("Token not found in header.") { }
    }
}
