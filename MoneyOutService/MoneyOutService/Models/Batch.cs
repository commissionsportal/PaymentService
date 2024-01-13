using System.ComponentModel.DataAnnotations;

namespace MoneyOutService.Models
{
    public class Batch
    {
        [Key]
        public string Id { get; set; }
        public DateTime? Created { get; set; }
    }
}