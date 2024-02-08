using Newtonsoft.Json;

namespace PaymentService.Models.PaymentureWallet
{
    public class TokenRequest
    {
        [JsonProperty("grant_type")]
        public string GrantType { get; set; } = "password";

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }
    }
}