using Microsoft.Extensions.Options;
using PaymentService.Interfaces;
using PaymentService.Models;
using PaymentService.Options;

namespace PaymentService.Services
{
    public class BatchService : IBatchService
    {
        private readonly IClient _client;
        private readonly PaymentureMoneyOutServiceOptions _options;
        private readonly IPaymentureWalletService _paymentureWalletService;

        public BatchService(IClient client, IOptions<PaymentureMoneyOutServiceOptions> options, IPaymentureWalletService paymentureWalletService)
        {
            _client = client;
            _options = options.Value;
            _paymentureWalletService = paymentureWalletService;
        }

        public async Task ProcessBatch(Batch batch)
        {
            string formattedUrlQueryIds = string.Join("&", batch.Releases.Select(x => $"ids={x.NodeId}"));
            var customerDetails = await _client.Get<CustomerDetails[]>($"{_options.PillarsApiUrl}/api/v1/Customers?{formattedUrlQueryIds}");
            await _paymentureWalletService.ProcessCommissionBatch(batch, customerDetails);
        }
    }
}