namespace MoneyOutService.Models
{
    public class UnreleasedBonus
    {
        public string BonusId { get; set; } = string.Empty;


        public string BonusTitle { get; set; } = string.Empty;


        public string NodeId { get; set; } = string.Empty;


        public long PeriodId { get; set; }

        public string Description { get; set; } = string.Empty;


        public decimal Amount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public decimal Volume { get; set; }

        public decimal Percent { get; set; }

        public decimal Released { get; set; }

        public int Level { get; set; }

        public DateTime CommissionDate { get; set; }

        public bool IsFirstTimeBonus { get; set; }
    }
}
