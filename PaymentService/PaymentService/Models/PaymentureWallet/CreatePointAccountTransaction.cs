namespace PaymentService.Models.PaymentureWallet
{
    public class CreatePointAccountTransaction
    {
        public string TransactionNumber { get; set; }
        public ResponseStatus Status { get; set; }
        public string ErrorDescription { get; set; }
        public string Message { get; set; }
    }
}
