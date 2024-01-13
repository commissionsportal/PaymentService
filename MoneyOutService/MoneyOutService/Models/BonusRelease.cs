namespace MoneyOutService.Models
{
    public class BonusRelease
    {
        public string BonusId { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public Status Status { get; set; }
    }
}
