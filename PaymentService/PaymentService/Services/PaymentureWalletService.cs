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
        private readonly PaymentureWalletServiceOptions _options;

        public PaymentureWalletService(IClient client, IHttpContextAccessor httpContextAccessor, IOptions<PaymentureWalletServiceOptions> options)
        {
            _client = client;
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
        }

        public async Task<List<StringResponse>> ProcessCommissionBatch(Batch batch, CustomerDetails[] customerDetails, HeaderData headerData)
        {
            var result = new List<StringResponse>();
            var token = await CreateToken(headerData);
            Dictionary<string, string> headers = new() { { "Authorization", $"Bearer {token}" } };
            var customersToVerify = new VerifyCustomersRequest { CompanyId = headerData.CompanyId, ExternalIds = batch.Releases.Select(x => x.NodeId).ToList() };
            var companyPointAccounts = await _client.Get<CompanyPointAccount>(headers, $"{_options.PaymentureBaseApiUrl}/api/CompanyPointAccount/GetCompanyPointAccounts?companyId={headerData.CompanyId}");

            if (companyPointAccounts == null)
            {
                return result;
            }
            //"[{\"data\":\"897F8AF401\",\"status\":\"Failed\",\"errorDescription\":null,\"message\":null,\"errorTransactionId\":null}]"
            var customerVerifications = await _client.PostJson<List<StringResponse>, VerifyCustomersRequest>(headers, $"{_options.PaymentureBaseApiUrl}/api/Customer/VerifyCustomers", customersToVerify);
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
                            ExternalCustomerID = details.Id,
                            CompanyID = headerData.CompanyId,
                            BackofficeID = details.Id,
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

            if (createCustomersRequest.Any())
            {
                var createCustomersResponse = await _client.PostJson<BooleanResponse, List<PaymentureCustomer>>(headers, $"{_options.PaymentureBaseApiUrl}/api/Customer/BulkCreateCustomers", createCustomersRequest);
            }

            var distinctBonuses = batch.Releases.Select(x => x.BonusId).Distinct();
            var distinctPeriods = batch.Releases.Select(x => x.PeriodId).Distinct();
            string bonusIdsQueryParams = string.Join("&", distinctBonuses.Select(x => $"bonusIds={x}"));
            var bonusTitles = await _client.Get<BonusTitles[]>(new Dictionary<string, string> { { "Authorization", $"Bearer {headerData.CallbackToken}" } }, $"https://api.pillarshub.com/api/v1/Bonuses/Titles?{bonusIdsQueryParams}");
            string periodsQueryParams = string.Join("&", distinctPeriods.Select(x => $"IdList={x}"));
            var compPeriods = await _client.Get<CompensationPlanPeriod[]>(new Dictionary<string, string> { { "Authorization", $"Bearer {headerData.CallbackToken}" } }, $"https://api.pillarshub.com/api/v1/CompensationPlans/0/Periods?{periodsQueryParams}");
            var clientIpAddress = Convert.ToString(_httpContextAccessor.HttpContext?.Request.Headers["X-Forwarded-For"]) ?? string.Empty;
            Guid guid = Guid.NewGuid();
            long ticksNow = DateTime.UtcNow.Ticks;
            var batchSession = $"{guid}-{ticksNow}";
            List<CustomerPointTransactionsRequest> payoutBatchRequest = batch.Releases.Select(transaction => new CustomerPointTransactionsRequest
            {
                Amount = (double)transaction.Amount,
                BatchId = batch.Id,
                Comment = $"{bonusTitles.FirstOrDefault(x => x.BonusId == transaction.BonusId)?.Title} {compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.Begin}-{compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.End}",
                CompanyId = headerData.CompanyId,
                ExternalCustomerID = transaction.NodeId,
                PointAccountID = companyPointAccounts.Data.Find(x => x.CurrencyCode.Equals(transaction.Currency, StringComparison.InvariantCultureIgnoreCase))?.Id,
                RedeemType = RedeemType.Commission,
                ReferenceNo = $"{transaction.BonusId} | {transaction.NodeId} | {transaction.Currency}", // Concat BonusId, NodeId, Currency
                Source = "Pillars",
                TransactionType = TransactionType.Credit
            }).ToList();
            //var pointTransactionsResult = await CreatePointTransactionBulk(payoutBatchRequest);
            var createTransResults = new List<CreatePointAccountTransaction>();

            try
            {
                var response = await _client.PostJson<List<StringResponse>, List<CustomerPointTransactionsRequest>>(headers, $"{_options.PaymentureBaseApiUrl}/api/CustomerPointTransactions/BulkCreatePointTransaction", payoutBatchRequest);

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
                TokenRequest trequest = new() { client_id = headerData.ClientId, username = headerData.User, password = headerData.Token };
                var response = await _client.Post<TokenResponse, TokenRequest>(null, $"{_options.PaymentureBaseApiUrl}/token", trequest);

                if (string.IsNullOrWhiteSpace(response?.access_token))
                {
                    throw new Exception("Failed to obtain Paymenture API token.");
                }

                return response.access_token;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}