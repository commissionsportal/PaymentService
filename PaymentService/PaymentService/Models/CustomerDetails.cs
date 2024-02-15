namespace PaymentService.Models
{
    public class CustomerDetails
    {
        public string Id { get; set; } = string.Empty;
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
}