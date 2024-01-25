namespace PaymentService.Models.PaymentureWallet
{
    public class StringResponse : ResponseBase
    {
        public StringResponse() 
        {
            Data = string.Empty;
        }
        public string Data { get; set; }
    }
}