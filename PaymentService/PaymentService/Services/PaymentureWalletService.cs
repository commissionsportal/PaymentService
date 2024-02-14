using Microsoft.Extensions.Options;
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

        public async Task<List<StringResponse>> ProcessCommissionBatch(string clientId, Batch batch, CustomerDetails[] customerDetails, HeaderData headerData)
        {
            var result = new List<StringResponse>();
            var customersToVerify = new VerifyCustomersRequest { CompanyId = clientId, ExternalIds = batch.Releases.Select(x => x.NodeId).ToList() };
            var companyPointAccounts = await _client.Get<List<CompanyPointAccount>>(null, $"{_options.PaymentureBaseApiUrl}/api/CompanyPointAccount/GetCompanyPointAccounts?companyId={clientId}");

            if (companyPointAccounts == null)
            {
                return result;
            }

            var token = await CreateToken(headerData);
            Dictionary<string, string> headers = new() { { "Authorization", token } };
            var customerVerifications = await _client.Post<List<StringResponse>, VerifyCustomersRequest>(headers, $"{_options.PaymentureBaseApiUrl}/api/Customer/VerifyCustomers", customersToVerify);
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
                            CompanyID = clientId,
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

            var createCustomersResponse = await _client.Post<BooleanResponse, List<PaymentureCustomer>>(headers, $"{_options.PaymentureBaseApiUrl}/api/Customer/BulkCreateCustomers", createCustomersRequest);
            var distinctBonuses = batch.Releases.Select(x => x.BonusId).Distinct();
            var distinctPeriods = batch.Releases.Select(x => x.PeriodId).Distinct();
            string bonusIdsQueryParams = string.Join("&", distinctBonuses.Select(x => $"bonusIds={x}"));
            var bonusTitles = await _client.Get<BonusTitles[]>(null, $"https://api.pillarshub.com/{headerData.CallbackToken}/api/v1/Bonuses/Titles?{bonusIdsQueryParams}");
            string periodsQueryParams = string.Join("&", distinctPeriods.Select(x => $"IdList={x}"));
            var compPeriods = await _client.Get<CompensationPlanPeriod[]>(null, $"https://api.pillarshub.com/{headerData.CallbackToken}/api/v1/CompensationPlans/0/Periods?{periodsQueryParams}");
            var clientIpAddress = Convert.ToString(_httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"]) ?? string.Empty;
            Guid guid = Guid.NewGuid();
            long ticksNow = DateTime.UtcNow.Ticks;
            var batchSession = $"{guid}-{ticksNow}";
            List<CustomerPointTransactionsRequest> payoutBatchRequest = batch.Releases.Select(transaction => new CustomerPointTransactionsRequest
            {
                Amount = (double)transaction.Amount,
                BatchId = batch.Id,
                Comment = $"{bonusTitles.FirstOrDefault(x => x.BonusId == transaction.BonusId)?.Title} {compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.Begin}-{compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.End}",
                CompanyId = clientId,
                ExternalCustomerID = transaction.NodeId,
                PointAccountID = companyPointAccounts.Find(x => x.Data.CurrencyCode.Equals(transaction.Currency, StringComparison.InvariantCultureIgnoreCase))?.Data?.Id,
                RedeemType = RedeemType.Commission,
                ReferenceNo = $"{transaction.BonusId} | {transaction.NodeId} | {transaction.Currency}", // Concat BonusId, NodeId, Currency
                Source = "Pillars",
                TransactionType = TransactionType.Credit
            }).ToList();
            //var pointTransactionsResult = await CreatePointTransactionBulk(payoutBatchRequest);
            var createTransResults = new List<CreatePointAccountTransaction>();

            try
            {
                var response = await _client.Post<List<StringResponse>, List<CustomerPointTransactionsRequest>>(null, $"{_options.PaymentureBaseApiUrl}/api/CustomerPointTransactions/BulkCreatePointTransaction", payoutBatchRequest);

                foreach (var item in response)
                {
                    try
                    {
                        createTransResults.Add(new CreatePointAccountTransaction { TransactionNumber = item.Data, Status = item.Status, Message = item.Message });
                    }
                    catch (Exception)
                    {
                        createTransResults.Add(new CreatePointAccountTransaction { TransactionNumber = item.Data, Status = ResponseStatus.Failed, Message = item.Message });
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            foreach (var res in createTransResults)
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

        public async Task<string> CreateToken(HeaderData headerData)
        {
            try
            {
                TokenRequest trequest = new TokenRequest { ClientId = headerData.CallbackToken, Username = headerData.User, Password = headerData.Token };
                var apiUrl = $"{_options.PaymentureBaseApiUrl}/token";
                var response = await _client.Post<string, TokenRequest>(null, "url", trequest);

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}