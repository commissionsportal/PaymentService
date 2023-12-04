namespace MoneyOutService.Models
{
    public class ReleaseResult
    {
        public string? BatchId { get; set; } = null;
        public decimal Amount { get; set; }
        public long DetailId { get; set; }
        public long PeriodId { get; set; }
        public string BonusId { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
    }
}
