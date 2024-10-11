namespace PaymentService.Models.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException() : base("User not found in header.") { }
    }
}
