using Newtonsoft.Json;

namespace PaymentService.Models.PaymentureWallet
{
    public class TokenRequest
    {
        public string grant_type { get; set; } = "password";
        public string username { get; set; }
        public string password { get; set; }
        public string client_id { get; set; }
    }
}