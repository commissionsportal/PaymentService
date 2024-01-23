namespace MoneyOutService.Models
{
    public class CustomerDetails
    {
        public string Id { get; set; } = string.Empty;
        public List<string> ExternalIds { get; set; } = new List<string>();
        public int CustomerType { get; set; }
        public DateTime SignupDate { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public List<Address> Addresses { get; set; } = new List<Address>();
        public List<PhoneNumber> PhoneNumbers { get; set; } = new List<PhoneNumber>();
        public string EmailAddress { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string ProfileImage { get; set; } = string.Empty;
        public string WebAlias { get; set; } = string.Empty;
        public string CustomData { get; set; } = string.Empty;
        public MerchantData MerchantData { get; set; } = new MerchantData();
    }

    public class Address
    {
        public string Type { get; set; } = string.Empty;
        public string Line1 { get; set; } = string.Empty;
        public string Line2 { get; set; } = string.Empty;
        public string Line3 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
    }

    public class PhoneNumber
    {
        public string Number { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
    }

    public class MerchantData
    {
        public BankAccount BankAccount { get; set; } = new BankAccount();
    }

    public class BankAccount
    {
        public string CustomerId { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public string RoutingNumber { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string NameOnAccount { get; set; } = string.Empty;
        public string Other { get; set; } = string.Empty;
        public string CustomData { get; set; } = string.Empty;
    }
}