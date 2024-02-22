using Newtonsoft.Json;
using PaymentService.Interfaces;
using PaymentService.Models;
using PaymentService.Models.PaymentureWallet;

namespace PaymentService.Services
{
    public class BatchService : IBatchService
    {
        private readonly IClient _client;
        private readonly IPaymentureWalletService _paymentureWalletService;

        public BatchService(IClient client, IPaymentureWalletService paymentureWalletService)
        {
            _client = client;
            _paymentureWalletService = paymentureWalletService;
        }

        public async Task ProcessBatch(Batch batch, HeaderData headerData)
        {
            var token = await CreateToken(headerData);
            Dictionary<string, string> headers = new() { { "Authorization", $"Bearer {token}" } };
            var activityLog = new ActivityLogRequest
            {
                APIName = "ProcessBatch",
                CompanyId = headerData.CompanyId,
                Status = 200,
                Request = $"ProcessBatch entered. Request Content : {JsonConvert.SerializeObject(batch)}",
                RequestDateTime = DateTime.Now
            };
            await _client.PostJson<BooleanResponse, ActivityLogRequest>(headers, $"https://zippyapi.paymenture.com/api/ActivityLog/CreateActivityLog", activityLog);

            string formattedUrlQueryIds = string.Join("&", batch.Releases.Select(x => $"ids={x.NodeId}"));
            var customerDetails = await _client.Get<CustomerDetails[]>(new Dictionary<string, string> { { "Authorization", $"Bearer {headerData.CallbackToken}" } }, $"https://api.pillarshub.com/api/v1/Customers?{formattedUrlQueryIds}");
            await _paymentureWalletService.ProcessCommissionBatch(batch, customerDetails, headerData);
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