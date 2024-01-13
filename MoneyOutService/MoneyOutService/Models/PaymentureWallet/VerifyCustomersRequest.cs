namespace MoneyOutService.Models.PaymentureWallet
{
    public class VerifyCustomersRequest
    {
        public string CompanyId { get; set; } = string.Empty;
        public List<string> ExternalIds { get; set; } = new List<string>();
    }
}