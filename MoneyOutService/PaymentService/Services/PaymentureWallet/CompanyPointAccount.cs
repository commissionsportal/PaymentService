using Newtonsoft.Json;

namespace PaymentService.Models.PaymentureWallet
{
    public class CompanyPointAccount
    {
        [JsonProperty("data")]
        public CompanyPointAccountData Data { get; set; }
    }

    public class CompanyPointAccountData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("accountName")]
        public string AccountName { get; set; }

        [JsonProperty("currencyCode")]
        public string CurrencyCode { get; set; }
    }
}