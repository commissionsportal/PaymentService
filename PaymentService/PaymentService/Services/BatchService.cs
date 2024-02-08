using Microsoft.Extensions.Options;
using PaymentService.Interfaces;
using PaymentService.Models;
using PaymentService.Models.Exceptions;
using PaymentService.Models.PaymentureWallet;
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

        public async Task ProcessBatch(string clientId, Batch batch, HeaderData headerData)
        {
            var settings = await _client.Get<Settings>(null, $"{_options.PaymentureApiUrl}/api/ClientSetting/GetAllCompanySetting");
            
            if (settings.Status != ResponseStatus.Success)
            {
                throw new NotFoundException("Client settings could not be found.");
            }

            string formattedUrlQueryIds = string.Join("&", batch.Releases.Select(x => $"ids={x.NodeId}"));
            var customerDetails = await _client.Get<CustomerDetails[]>(new Dictionary<string, string> { { "Authorization", $"Bearer {headerData.CallbackToken}" } }, $"https://api.pillarshub.com/api/v1/Customers?{formattedUrlQueryIds}");
            await _paymentureWalletService.ProcessCommissionBatch(settings, clientId, batch, customerDetails, headerData);
        }
    }
}