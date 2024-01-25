using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PaymentService.Interfaces;
using PaymentService.Models;
using PaymentService.Models.PaymentureWallet;
using PaymentService.Options;

namespace PaymentService.Services
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

        public async Task<List<StringResponse>> ProcessCommissionBatch(Batch batch, CustomerDetails[] customerDetails)
        {
            var result = new List<StringResponse>();
            var customersToVerify = new VerifyCustomersRequest { CompanyId = _options.CompanyId, ExternalIds = batch.Releases.Select(x => x.NodeId).ToList() };
            var companyPointAccounts = await _client.Get<List<CompanyPointAccount>>($"{_options.PaymentureApiUrl}/api/CompanyPointAccount/GetCompanyPointAccounts?companyId={_options.CompanyId}");

            if (companyPointAccounts == null)
            {
                return result;
            }

            var customerVerifications = await _client.Post<List<StringResponse>, VerifyCustomersRequest>($"{_options.PaymentureApiUrl}/api/Customer/VerifyCustomers", customersToVerify);
            var createCustomersRequest = new List<PaymentureCustomer>();

            foreach (var customer in customerVerifications.Where(x => x.Status == ResponseStatus.Failed))
            {
                try
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
                            PhoneNumber = details.PhoneNumbers?.FirstOrDefault()?.Number,
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
                catch
                {
                    //log or something. The payout to this rep will likely fail
                }
            }

            var createCustomersResponse = await _client.Post<BooleanResponse, List<PaymentureCustomer>>($"{_options.PaymentureApiUrl}/api/Customer/BulkCreateCustomers", createCustomersRequest);

            var distinctBonuses = batch.Releases.Select(x => x.BonusId).Distinct();
            var distinctPeriods = batch.Releases.Select(x => x.PeriodId).Distinct();
            string bonusIdsQueryParams = string.Join("&", distinctBonuses.Select(x => $"bonusIds={x}"));
            var bonusTitles = await _client.Get<BonusTitles[]>($"{_options.PillarsApiUrl}/api/v1/Bonuses/Titles?{bonusIdsQueryParams}");
            string periodsQueryParams = string.Join("&", distinctPeriods.Select(x => $"IdList={x}"));
            var compPeriods = await _client.Get<CompensationPlanPeriod[]>($"{_options.PillarsApiUrl}/api/v1/CompensationPlans/0/Periods?{periodsQueryParams}");

            var clientIpAddress = Convert.ToString(_httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"]) ?? string.Empty;
            Guid guid = Guid.NewGuid();
            long ticksNow = DateTime.UtcNow.Ticks;
            var batchSession = $"{guid}-{ticksNow}";
            List<CustomerPointTransactionsRequest> payoutBatchRequest = batch.Releases.Select(transaction => new CustomerPointTransactionsRequest
            {
                Amount = (double)transaction.Amount,
                BatchId = batch.Id,
                Comment = $"{bonusTitles.FirstOrDefault(x => x.BonusId == transaction.BonusId)?.Title} {compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.Begin}-{compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.End}",
                CompanyId = _options.CompanyId,
                ExternalCustomerID = transaction.NodeId,
                PointAccountID = companyPointAccounts.Find(x => x.Data.CurrencyCode.Equals(transaction.Currency, StringComparison.InvariantCultureIgnoreCase))?.Data?.Id,
                RedeemType = RedeemType.Commission,
                ReferenceNo = $"{transaction.BonusId} | {transaction.NodeId} | {transaction.Currency}", // Concat BonusId, NodeId, Currency
                Source = "Pillars",
                TransactionType = TransactionType.Credit
            }).ToList();

            var pointTransactionsResult = await CreatePointTransactionBulk(payoutBatchRequest);

            foreach ( var res in pointTransactionsResult)
            {
                result.Add(new StringResponse
                {
                    Data = res.TransactionNumber,
                    Status = res.Status,
                    Message = res.Message
                });
            }

            return result;
        }

        private async Task<List<CreatePointAccountTransaction>> CreatePointTransactionBulk(List<CustomerPointTransactionsRequest> request)
        {
            var result = new List<CreatePointAccountTransaction>();

            try
            {
                var response = await _client.Post<List<StringResponse>, List<CustomerPointTransactionsRequest>>($"{_options.PaymentureApiUrl}/api/CustomerPointTransactions/BulkCreatePointTransaction", request);

                foreach (var item in response)
                {
                    try
                    {
                        result.Add(new CreatePointAccountTransaction { TransactionNumber = item.Data, Status = item.Status, Message = item.Message });
                    }
                    catch (Exception)
                    {
                        result.Add(new CreatePointAccountTransaction { TransactionNumber = item.Data, Status = ResponseStatus.Failed, Message = item.Message });
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }
    }
}