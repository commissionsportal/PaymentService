namespace MoneyOutService.Models
{
    public class CompensationPlanPeriod
    {
        public int Id { get; set; }
        public DateTime Begin { get; set; }
        public DateTime End { get; set; }
        public int CompensationPlanId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int SnapshotId { get; set; }
    }
}
