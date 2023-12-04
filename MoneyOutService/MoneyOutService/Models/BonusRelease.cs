namespace MoneyOutService.Models
{
    public class BonusRelease
    {
        public int Id { get; set; }
        public string BonusId { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public long PeriodId { get; set; }
        public long DetailId { get; set; }
        public decimal Amount { get; set; }
    }
}
