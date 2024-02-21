using Newtonsoft.Json;

namespace PaymentService.Models
{
    public class CompensationPlanPeriod
    {
        [JsonProperty("id")]
        public int Id { get; set; }
        
        [JsonProperty("begin")]
        public string Begin { get; set; }
        
        [JsonProperty("end")]
        public string End { get; set; }
        
        [JsonProperty("compensationPlanId")]
        public int CompensationPlanId { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;
    }
}
