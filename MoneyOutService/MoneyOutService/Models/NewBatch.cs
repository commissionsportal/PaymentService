using System;

namespace MoneyOutService.Models
{
    public class NewBatch
    {
        public DateTime? CutoffDate { get; set; } = null;
        public string[]? BonusTitles { get; set; } = null;
        public string[]? NodeIds { get; set; } = null;
    }
}
