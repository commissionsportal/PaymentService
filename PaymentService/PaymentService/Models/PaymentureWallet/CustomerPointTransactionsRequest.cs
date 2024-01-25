namespace PaymentService.Models.PaymentureWallet
{
    public class CustomerPointTransactionsRequest
    {
        public string Id { get; set; }
        public string CompanyId { get; set; }
        public string ExternalCustomerID { get; set; }
        public string PointAccountID { get; set; }
        public TransactionType TransactionType { get; set; }
        public RedeemType RedeemType { get; set; }
        public string ReferenceNo { get; set; }
        public double Amount { get; set; }
        public string Comment { get; set; }
        public string BatchId { get; set; }
        public string Source { get; set; }
    }

    public static class CommissionPayoutStatus
    {
        public const string Pending = "pending";
        public const string Success = "success";
        public const string Failed = "failed";
    }

    public enum RedeemType
    {
        Initial,
        Order,
        Autoship,
        SignupFee,
        Transfer,
        Adjust,
        Other,
        IPayoutTransferfee,
        ACHTransferfee,
        CambridgeTransferfee,
        CheckTransferfee,
        Commission,
        Refund
    }

    public enum TransactionType
    {
        Credit,
        Debit,
    }
}
