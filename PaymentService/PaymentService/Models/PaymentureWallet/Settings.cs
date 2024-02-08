namespace PaymentService.Models.PaymentureWallet
{
    public class Settings : ResponseBase
    {
        public List<SettingDetail> Data { get; set; }
    }

    public class SettingDetail
    {
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
    }
}
