namespace PaymentService.Models.PaymentureWallet
{
    public class ActivityLogRequest
    {
        public string APIName { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public int Status { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string HeaderReferer { get; set; }
        public string ErrorDescription { get; set; }
        public DateTime RequestDateTime { get; set; }
        public DateTime ResponseDateTime { get; set; }
    }
}