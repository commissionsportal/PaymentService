using Newtonsoft.Json;
using PaymentService.Interfaces;
using PaymentService.Models;
using PaymentService.Models.PaymentureWallet;

namespace PaymentService.Services
{
    public class PaymentureWalletService : IPaymentureWalletService
    {
        private readonly IClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentureWalletService(IClient client, IHttpContextAccessor httpContextAccessor)
        {
            _client = client;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<StringResponse>> ProcessCommissionBatch(Batch batch, CustomerDetails[] customerDetails, HeaderData headerData)
        {
            var token = await CreateToken(headerData);
            Dictionary<string, string> headers = new() { { "Authorization", $"Bearer {token}" } };
            var result = new List<StringResponse>();

            try
            {
                var activityLog = new ActivityLogRequest
                {
                    APIName = "ProcessCommissionBatch",
                    CompanyId = headerData.CompanyId,
                    Status = 200,
                    Request = $"ProcessCommissionBatch Step 1. Customer Details: {JsonConvert.SerializeObject(customerDetails)}",
                    RequestDateTime = DateTime.Now
                };
                await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);

                var customersToVerify = new VerifyCustomersRequest { CompanyId = headerData.CompanyId, ExternalIds = batch.Releases.Select(x => x.NodeId).Distinct().ToList() };
                var companyPointAccounts = await _client.Get<CompanyPointAccount>(headers, $"https://zippyapi.paymenture.com/api/CompanyPointAccount/GetCompanyPointAccounts?companyId={headerData.CompanyId}");

                activityLog = new ActivityLogRequest
                {
                    APIName = "ProcessCommissionBatch",
                    CompanyId = headerData.CompanyId,
                    Status = 200,
                    Request = $"ProcessCommissionBatch Step 2. Company Point Accounts: {JsonConvert.SerializeObject(companyPointAccounts)}",
                    RequestDateTime = DateTime.Now
                };
                await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);

                if (companyPointAccounts == null)
                {
                    return result;
                }

                var customerVerifications = await _client.PostJson<List<StringResponse>, VerifyCustomersRequest>(headers, $"https://zippyapi.paymenture.com/api/Customer/VerifyCustomers", customersToVerify);
                var createCustomersRequest = new List<PaymentureCustomer>();

                activityLog = new ActivityLogRequest
                {
                    APIName = "ProcessCommissionBatch",
                    CompanyId = headerData.CompanyId,
                    Status = 200,
                    Request = $"ProcessCommissionBatch Step 3. Customer Verification: {JsonConvert.SerializeObject(customersToVerify)}",
                    Response = JsonConvert.SerializeObject(customerVerifications),
                    RequestDateTime = DateTime.Now
                };
                await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);

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
                                CountryCode = customerAddress?.CountryCode,
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
                    catch (Exception ex)
                    {
                        activityLog = new ActivityLogRequest
                        {
                            APIName = "ProcessCommissionBatch",
                            CompanyId = headerData.CompanyId,
                            Status = 500,
                            Request = $"Customer Verifications: {JsonConvert.SerializeObject(customerVerifications)}",
                            Response = ex.ToString(),
                            ErrorDescription = ex.Message,
                            RequestDateTime = DateTime.Now
                        };
                        await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);
                    }
                }

                activityLog = new ActivityLogRequest
                {
                    APIName = "ProcessCommissionBatch",
                    CompanyId = headerData.CompanyId,
                    Status = 200,
                    Request = $"ProcessCommissionBatch Step 4. Customers to create: {JsonConvert.SerializeObject(createCustomersRequest)}",
                    RequestDateTime = DateTime.Now
                };
                await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);

                if (createCustomersRequest.Any())
                {
                    try
                    {
                        var createCustomersResponse = await _client.PostJson<List<StringResponse>, List<PaymentureCustomer>>(headers, $"https://zippyapi.paymenture.com/api/v2/Customer/BulkCreateCustomers", createCustomersRequest);

                        activityLog = new ActivityLogRequest
                        {
                            APIName = "ProcessCommissionBatch",
                            CompanyId = headerData.CompanyId,
                            Status = 200,
                            Request = $"ProcessCommissionBatch Step 5. Customers to create: {JsonConvert.SerializeObject(createCustomersRequest)}",
                            Response = JsonConvert.SerializeObject(createCustomersResponse),
                            RequestDateTime = DateTime.Now
                        };
                        await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);
                    }
                    catch (Exception ex)
                    {
                        activityLog = new ActivityLogRequest
                        {
                            APIName = "ProcessCommissionBatch",
                            CompanyId = headerData.CompanyId,
                            Status = 500,
                            Request = $"Customer Creation: {JsonConvert.SerializeObject(createCustomersRequest)}",
                            Response = ex.ToString(),
                            ErrorDescription = ex.Message,
                            RequestDateTime = DateTime.Now
                        };
                        await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);
                    }
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
                List<CustomerPointTransactionsRequest> payoutBatchRequest = new();

                foreach (var transaction in batch.Releases)
                {
                    string periodBegin = compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.Begin;
                    string periodEnd = compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.End;

                    try
                    {
                        DateTime dt = DateTime.Parse(compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.Begin);
                        periodBegin = dt.ToString("yyyy/MM/dd");
                        dt = DateTime.Parse(compPeriods.FirstOrDefault(x => x.Id == transaction.PeriodId)?.End);
                        periodEnd = dt.ToString("yyyy/MM/dd");
                    }
                    catch (Exception ex)
                    {
                        activityLog = new ActivityLogRequest
                        {
                            APIName = "ProcessCommissionBatch",
                            CompanyId = headerData.CompanyId,
                            Status = 500,
                            Request = "Failed parsing dates",
                            Response = ex.ToString(),
                            ErrorDescription = ex.Message,
                            RequestDateTime = DateTime.Now
                        };
                        await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);
                    }

                    payoutBatchRequest.Add(
                        new CustomerPointTransactionsRequest
                        {
                            Amount = (double)transaction.Amount,
                            BatchId = batch.Id,
                            Comment = $"{bonusTitles.FirstOrDefault(x => x.BonusId == transaction.BonusId)?.Title} {periodBegin}-{periodEnd}",
                            CompanyId = headerData.CompanyId,
                            ExternalCustomerID = transaction.NodeId,
                            PointAccountID = companyPointAccounts.Data.Find(x => x.CurrencyCode.Equals(transaction.Currency, StringComparison.InvariantCultureIgnoreCase))?.Id,
                            RedeemType = RedeemType.Commission,
                            ReferenceNo = $"{transaction.BonusId} | {transaction.NodeId} | {transaction.Currency}", // Concat BonusId, NodeId, Currency
                            Source = "Pillars",
                            TransactionType = TransactionType.Credit
                        });
                }
                var createTransResults = new List<CreatePointAccountTransaction>();

                try
                {
                    activityLog = new ActivityLogRequest
                    {
                        APIName = "ProcessCommissionBatch",
                        CompanyId = headerData.CompanyId,
                        Status = 200,
                        Request = $"ProcessCommissionBatch Step 6. Payout Batch Request: {JsonConvert.SerializeObject(payoutBatchRequest)}",
                        RequestDateTime = DateTime.Now
                    };
                    await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);

                    var response = await _client.PostJson<List<StringResponse>, List<CustomerPointTransactionsRequest>>(headers, $"https://zippyapi.paymenture.com/api/v2/CustomerPointTransactions/BulkCreatePointTransaction", payoutBatchRequest);

                    activityLog = new ActivityLogRequest
                    {
                        APIName = "ProcessCommissionBatch",
                        CompanyId = headerData.CompanyId,
                        Status = 200,
                        Request = $"ProcessCommissionBatch Step 7. Payout Batch Request: {JsonConvert.SerializeObject(payoutBatchRequest)}",
                        Response = JsonConvert.SerializeObject(response),
                        RequestDateTime = DateTime.Now
                    };
                    await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);

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
                catch (Exception ex)
                {
                    activityLog = new ActivityLogRequest
                    {
                        APIName = "ProcessCommissionBatch",
                        CompanyId = headerData.CompanyId,
                        Status = 500,
                        Request = $"Create Transactions: {JsonConvert.SerializeObject(payoutBatchRequest)}",
                        Response = ex.ToString(),
                        ErrorDescription = ex.Message,
                        RequestDateTime = DateTime.Now
                    };
                    await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);

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
            }
            catch (Exception ex)
            {
                var activityLog = new ActivityLogRequest
                {
                    APIName = "ProcessCommissionBatch",
                    CompanyId = headerData.CompanyId,
                    Status = 500,
                    Request = "Unknown error",
                    Response = ex.ToString(),
                    ErrorDescription = ex.Message,
                    RequestDateTime = DateTime.Now
                };
                await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);
            }

            return result;
        }

        public async Task<string> CreateToken(HeaderData headerData)
        {
            try
            {
                TokenRequest trequest = new() { client_id = headerData.ClientId, username = headerData.User, password = headerData.Token };
                var response = await _client.Post<TokenResponse, TokenRequest>(null, $"https://zippyapi.paymenture.com/token", trequest);

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