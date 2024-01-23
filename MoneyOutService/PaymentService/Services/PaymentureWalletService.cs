using Microsoft.Extensions.Options;
using MoneyOutService.Interfaces;
using MoneyOutService.Models;
using MoneyOutService.Models.PaymentureWallet;
using MoneyOutService.Options;
using System;

namespace MoneyOutService.Services
{
    public class PaymentureWalletService : IPaymentureWalletService
    {
        private readonly IClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PaymentureMoneyOutServiceOptions _options;

        public PaymentureWalletService(IClient client, IHttpContextAccessor httpContextAccessor, IOptions<PaymentureMoneyOutServiceOptions> options)
        {
            _client = client;
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
        }

        public async Task<BatchResult> CreateBatch(List<BonusRelease> releases)
        {
            var companyPointAccounts = await _client.GetValue<List<CompanyPointAccount>>($"{_options.PaymentureApiUrl}/api/CompanyPointAccount/GetCompanyPointAccounts?companyId={_options.CompanyId}");

            if (companyPointAccounts == null )
            {
                return new BatchResult();
            }

            BatchResult result = new();
            var clientIpAddress = Convert.ToString(_httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"]) ?? string.Empty;
            Guid guid = Guid.NewGuid();
            long ticksNow = DateTime.UtcNow.Ticks;
            var batchSession = $"{guid}-{ticksNow}";
            List<CommissionPayout> payoutBatchRequest = releases.Select(transaction => new CommissionPayout
            {
                BatchSession = batchSession,
                Amount = (double)transaction.Amount,
                ApprovedBy = "Automatic",
                BatchId = string.Empty,
                Comment = string.Empty,
                CompanyId = _options.CompanyId,
                ExternalCustomerID = transaction.NodeId,
                IsHoldAmount = false,
                PointAccountID = companyPointAccounts.Find(x => x.Data.CurrencyCode.Equals(transaction.Currency, StringComparison.InvariantCultureIgnoreCase))?.Data?.Id, //Needs to be set based on currency
                RedeemType = 11, // Commission
                ReferenceNo = $"{transaction.BonusId} | {transaction.NodeId} | {transaction.Currency}", // Concat BonusId, NodeId, Currency
                Source = "Pillars",
                Status = CommissionPayoutStatus.Pending,
                Response = "",
                TransactionType = TransactionType.Credit,
                IpAddress = clientIpAddress,
            }).ToList();

            var saveBatchResult = await _client.Post<List<StringResponse>, List<CommissionPayout>>($"{_options.PaymentureApiUrl}/api/CommssionPayout/SaveCommissionPayoutList",
               payoutBatchRequest);

            //validate the result before returning
            if (saveBatchResult.Count > 0 && saveBatchResult.FirstOrDefault()?.Status == ResponseStatus.Success)
            {
                result.BatchId = saveBatchResult.FirstOrDefault().Data;
                result.BatchSession = batchSession;
            }

                return result;
        }

        public async Task<List<StringResponse>> ProcessCommissionBatch(List<BonusRelease> releases, BatchResult batchInfo, CustomerDetails[] customerDetails)
        {
            var customersToVerify = new VerifyCustomersRequest { CompanyId = _options.CompanyId, ExternalIds = releases.Select(x => x.NodeId).ToList() };
            var customerVerifications = await _client.Post<List<StringResponse>, VerifyCustomersRequest>($"{_options.PaymentureApiUrl}/api/Customer/VerifyCustomers", customersToVerify);
            var createCustomersRequest = new List<PaymentureCustomer>();

            foreach (var customer in customerVerifications.Where(x => x.Status == ResponseStatus.Failed))
            {
                var details = customerDetails.FirstOrDefault(x => x.Id == customer.Data);

                if (details != null)
                {
                    var customerAddress = details.Addresses.FirstOrDefault();

                    createCustomersRequest.Add(new PaymentureCustomer
                    {
                        ExternalCustomerID = customer.Data,
                        CompanyID = _options.CompanyId,
                        BackofficeID = customer.Data,
                        FirstName = details.FirstName,
                        LastName = details.LastName,
                        CustomerType = "0",
                        CustomerLanguage = details.Language,
                        EmailAddress = details.EmailAddress,
                        PhoneNumber = details.PhoneNumbers.FirstOrDefault()?.Number,
                        DateOfBirth = details.BirthDate,
                        Address = new PaymentureAddress
                        {
                            Address1 = customerAddress?.Line1,
                            Address2 = customerAddress?.Line2,
                            City = customerAddress?.City,
                            State = customerAddress?.StateCode,
                            CountryCode = customerAddress?.CountryCode,
                            Zip = customerAddress?.Zip
                        }
                    });
                }
            }

            var result = await _client.Post<BooleanResponse, List<PaymentureCustomer>>($"{_options.PaymentureApiUrl}/api/Customer/BulkCreateCustomers", createCustomersRequest);

            try
            {
                //call api to process the commission batch payments
                return await _client.Post<List<StringResponse>, object>($"{_options.PaymentureApiUrl}/api/CommssionPayout/ProcessCommissionBatch?companyId={_options.CompanyId}&batchSession={batchInfo.BatchSession}&batchId={batchInfo.BatchId}", null);
            }
            catch (Exception ex)
            {
                return new List<StringResponse>();
            }

        }
    }
}