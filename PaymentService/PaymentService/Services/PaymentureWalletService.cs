using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaymentService.Interfaces;
using PaymentService.Models;
using PaymentService.Models.PaymentureWallet;
using PaymentService.Options;
using System.Text;

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

        public async Task<List<StringResponse>> ProcessCommissionBatch(Settings settings, string clientId, Batch batch, CustomerDetails[] customerDetails, HeaderData headerData)
        {
            var result = new List<StringResponse>();
            var customersToVerify = new VerifyCustomersRequest { CompanyId = clientId, ExternalIds = batch.Releases.Select(x => x.NodeId).ToList() };
            var companyPointAccounts = await _client.Get<List<CompanyPointAccount>>(null, $"{_options.PaymentureApiUrl}/api/CompanyPointAccount/GetCompanyPointAccounts?companyId={clientId}");

            if (companyPointAccounts == null)
            {
                return result;
            }

            var customerVerifications = await _client.Post<List<StringResponse>, VerifyCustomersRequest>(null, $"{_options.PaymentureApiUrl}/api/Customer/VerifyCustomers", customersToVerify);
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

            var createCustomersResponse = await _client.Post<BooleanResponse, List<PaymentureCustomer>>(null, $"{_options.PaymentureApiUrl}/api/Customer/BulkCreateCustomers", createCustomersRequest);

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

            var pointTransactionsResult = await CreatePointTransactionBulk(payoutBatchRequest);

            foreach (var res in pointTransactionsResult)
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
                var response = await _client.Post<List<StringResponse>, List<CustomerPointTransactionsRequest>>(null, $"https://api.pillarshub.com/{_options.PaymentureApiUrl}/api/CustomerPointTransactions/BulkCreatePointTransaction", request);

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

        public dynamic CallEwallet(string requestUrl, object requestData, HeaderData headerData)
        {
            try
            {
                string token = CreateToken();
                var settings = _EwalletRepository.GetSettings();

                if (!string.IsNullOrEmpty(token))
                {
                    token = "Bearer " + token;
                }
                else
                {
                    throw new Exception($"Error occured at Method {System.Reflection.MethodBase.GetCurrentMethod().Name} and Error = Token is NULL!");
                }

                var jsonData = JsonConvert.SerializeObject(requestData);
                var apiUrl = settings.ApiUrl + requestUrl;
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("POST"), new Uri(apiUrl));
                request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var result = _httpClientService.MakeRequestByToken(request, "Authorization", token);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return result;
                }
                else if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new Exception($"Error occured at Method {System.Reflection.MethodBase.GetCurrentMethod().Name} and Error = Invalid Token!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occured at Method {System.Reflection.MethodBase.GetCurrentMethod().Name} and error ='{ex.Message}'");
            }
            return null;
        }

        public string CreateToken(HeaderData headerData)
        {
            try
            {
                TokenRequest trequest = new TokenRequest { ClientId = settings.CompanyId, Username = settings.Username, password = settings.Password };
                var jsonData = JsonConvert.SerializeObject(trequest);

                var apiUrl = settings.ApiUrl + "token";

                var data = _httpClientService.PostRequest(apiUrl, trequest);

                if (data?.StatusCode.ToString() == "OK")
                {
                    var jsonString = data?.Content?.ReadAsStringAsync();
                    jsonString.Wait();

                    var jobject = jsonString?.Result?.ToString();
                    dynamic jdata = JObject.Parse(jobject);
                    return jdata?.access_token;
                }
                else
                {
                    throw new Exception(data?.StatusCode.ToString() + data?.Content.ToString() + " URL: " + apiUrl + " json: " + jsonData + " reason: " + data.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}