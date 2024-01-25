using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PaymentService.Interfaces;
using PaymentService.Models;
using PaymentService.Models.PaymentureWallet;
using PaymentService.Options;

namespace PaymentService.Services
{
    public class BatchService : IBatchService
    {
        private readonly IClient _client;
        private readonly IBonusRepository _bonusRepository;
        private readonly PaymentureMoneyOutServiceOptions _options;
        private readonly IPaymentureWalletService _paymentureWalletService;

        public BatchService(IClient client, IBonusRepository bonusRepository, IOptions<PaymentureMoneyOutServiceOptions> options, IPaymentureWalletService paymentureWalletService)
        {
            _client = client;
            _bonusRepository = bonusRepository;
            _options = options.Value;
            _paymentureWalletService = paymentureWalletService;
        }

        public async Task ProcessBatch(Batch batch)
        {
            //Process the bonuses and mark them released.
            //var bonuses = batch.Releases.Select(x => { x.Status = Status.Success; return x; });
            //await _bonusRepository.UpdateBatch(batch.Id, bonuses);

            //var batchResult = await _paymentureWalletService.CreateBatch(batch.Releases.ToList());

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Factory.StartNew(async () =>
            {
                //get customer details incase we need to create wallet accounts
                string formattedUrlQueryIds = string.Join("&", batch.Releases.Select(x => $"ids={x.NodeId}"));
                var customerDetails = await _client.Get<CustomerDetails[]>($"{_options.PillarsApiUrl}/api/v1/Customers?{formattedUrlQueryIds}");

                // Process the commission batch
                var processPaymentsResult = await _paymentureWalletService.ProcessCommissionBatch(batch, customerDetails);

                //Iterate over releases and set Status based success of the payment on paymenture side
                //foreach (var paymentResult in processPaymentsResult)
                //{
                //    var bonus = batch.Releases.First(x => $"{x.BonusId} | {x.NodeId} | {x.Currency}" == paymentResult.Data);

                //    if (bonus != null)
                //    {
                //        bonus.Status = paymentResult.Status == ResponseStatus.Success ? Status.Success : Status.Failure;
                //    }
                //}

                //await _bonusRepository.UpdateBatch(batch.Id, batch.Releases.ToArray());

                //var distinctBonuses = batch.Releases.Select(x => x.BonusId).Distinct();
                //var distinctPeriods = batch.Releases.Select(x => x.PeriodId).Distinct();

                ////call /api/v1/Bonuses/Titles and pass distinctBonuses and take the 'title' to add to the transaction message
                ////call /api/v1/CompensationPlans/0/Periods and pass distinctPeriods and take the 'begin' and 'end' dates 
                //// take the 'title' and dates to make the transaction message
                //string bonusIdsQueryParams = string.Join("&", distinctBonuses.Select(x => $"bonusIds={x}"));
                //var bonusTitles = await _client.Get<BonusTitles[]>($"{_options.PillarsApiUrl}/api/v1/Bonuses/Titles?{bonusIdsQueryParams}");
                //string periodsQueryParams = string.Join("&", distinctPeriods.Select(x => $"IdList={x}"));
                //var compPeriods = await _client.Get<CompensationPlanPeriod[]>($"{_options.PillarsApiUrl}/api/v1/CompensationPlans/0/Periods?{periodsQueryParams}");

                //TODO: take the titles and period dates and update the paymenture transaction to have a meaningful message

                //take the 'released' values and update the batch payments with the resulting DetailId
                //var result = await _batchRepository.UpdateBatch(clientId, batchResult.BatchId, released.ToArray());
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        }
    }
}
