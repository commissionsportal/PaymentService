namespace MoneyOutService.Models.PaymentureWallet
{
    public class CommissionPayout
    {
        public string BatchSession { get; set; }
        public string CompanyId { get; set; }
        public string ExternalCustomerID { get; set; }
        public string PointAccountID { get; set; }
        public TransactionType TransactionType { get; set; }
        public int RedeemType { get; set; }
        public string ReferenceNo { get; set; }
        public double Amount { get; set; }
        public string Comment { get; set; }
        public string BatchId { get; set; }
        public string Source { get; set; }
        public bool IsHoldAmount { get; set; }
        public string IpAddress { get; set; }
        public string Status { get; set; }
        public string Response { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string ApprovedBy { get; set; }
    }

    public static class CommissionPayoutStatus
    {
        public const string Pending = "pending";
        public const string Success = "success";
        public const string Failed = "failed";
    }

    public enum TransactionType
    {
        Credit,
        Debit,
    }
}
