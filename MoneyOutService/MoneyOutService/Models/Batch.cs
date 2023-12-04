using System.ComponentModel.DataAnnotations;

namespace MoneyOutService.Models
{
    public class Batch
    {
        [Key]
        public long Id { get; set; }
        public DateTime? Created { get; set; }
        public BonusRelease[]? Bonuses { get; set; } = null;
    }

    public class BatchSummary
    {
        public long Id { get; set; }
        public DateTime? Created { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class BatchSummaryCount
    {
        public BatchSummary[] Batches { get; set; }
        public int Count { get; set; }
    }
}
