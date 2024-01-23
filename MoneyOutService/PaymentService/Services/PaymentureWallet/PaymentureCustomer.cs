namespace PaymentService.Models.PaymentureWallet
{
    public class PaymentureCustomer
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ExternalCustomerID { get; set; }
        public string BackofficeID { get; set; }
        public string CompanyID { get; set; }
        public int CustomerStatus { get; set; }
        public string CustomerType { get; set; }
        public string CustomerLanguage { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string CountryCode { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string KycStatus { get; set; }
        public string LanguageCode { get; set; }
        public PaymentureAddress Address { get; set; }
    }

    public class PaymentureAddress
    {
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string CountryCode { get; set; }
    }
}