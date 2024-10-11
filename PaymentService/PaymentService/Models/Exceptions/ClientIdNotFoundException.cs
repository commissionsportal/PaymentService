namespace PaymentService.Models.Exceptions
{
    public class ClientIdNotFoundException : Exception
    {
        public ClientIdNotFoundException() : base("ClientId not found in header.") { }
    }
}
