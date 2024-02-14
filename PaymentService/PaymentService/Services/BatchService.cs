using PaymentService.Interfaces;
using PaymentService.Models;

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

        public async Task ProcessBatch(string clientId, Batch batch, HeaderData headerData)
        {
            string formattedUrlQueryIds = string.Join("&", batch.Releases.Select(x => $"ids={x.NodeId}"));
            var customerDetails = await _client.Get<CustomerDetails[]>(new Dictionary<string, string> { { "Authorization", $"Bearer {headerData.CallbackToken}" } }, $"https://api.pillarshub.com/api/v1/Customers?{formattedUrlQueryIds}");
            await _paymentureWalletService.ProcessCommissionBatch(clientId, batch, customerDetails, headerData);
        }
    }
}