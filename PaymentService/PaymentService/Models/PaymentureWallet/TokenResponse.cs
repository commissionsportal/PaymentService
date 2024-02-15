using Newtonsoft.Json;

namespace PaymentService.Models.PaymentureWallet
{
    public class TokenResponse
    {
        public string token_type { get; set; }
        public string access_token { get; set; }
        public int expires_in { get; set; }
    }
}
