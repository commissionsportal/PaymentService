using Microsoft.Extensions.Options;
using MoneyOutService.Interfaces;
using MoneyOutService.Models;
using MoneyOutService.Models.PaymentureWallet;
using MoneyOutService.Options;
using System.Linq;

namespace MoneyOutService.Services
{
    public class BatchService : IBatchService
    {
        private readonly IBonusRepository _bonusRepository;
        private readonly IBatchRepository _batchRepository;
        private readonly IClient _client;
        private readonly PaymentureMoneyOutServiceOptions _options;
        private readonly IPaymentureWalletService _paymentureWalletService;

        public BatchService(IBonusRepository bonusRepository, IBatchRepository batchRepository, IClient client, IOptions<PaymentureMoneyOutServiceOptions> options, IPaymentureWalletService paymentureWalletService)
        {
            _bonusRepository = bonusRepository;
            _batchRepository = batchRepository;
            _client = client;
            _options = options.Value;
            _paymentureWalletService = paymentureWalletService;
        }

        public async Task<Batch?> CreateBatch(int clientId, NewBatch newBatch)
        {
            //Get a list of all Unrelased Bonuses
            var bonuses = await _bonusRepository.GetUnreleasedBonuses(clientId, newBatch.CutoffDate, newBatch.NodeIds, 0, -1);

            //Generate a bonus Hash based on bonuses requested. Default to all bonuses.
            var bonusHash = newBatch.BonusTitles?.Distinct().ToHashSet() ?? new HashSet<string>();
            if (newBatch.BonusTitles == null || newBatch.BonusTitles.Length == 0)
            {
                bonusHash = bonuses.Select(x => x.BonusTitle).Distinct().ToHashSet();
            }

            //Generate a list of customers to release bonuses for.
            var customerIds = newBatch.NodeIds?.Distinct() ?? Array.Empty<string>();
            if (newBatch.NodeIds == null || newBatch.NodeIds.Length == 0)
            {
                customerIds = bonuses.Select(x => x.NodeId).Distinct();
            }

            //Group the bonuses into releases. Filter by Bonus Titles and Customer Ids.
            List<BonusRelease> releases = new();

            foreach (var customerid in customerIds)
            {
                var customerSum = bonuses.Where(x => x.NodeId == customerid && bonusHash.Contains(x.BonusTitle)).Sum(x => x.Amount);

                if (customerSum > 0)
                {
                    releases.AddRange(bonuses.Where(x => x.NodeId == customerid && bonusHash.Contains(x.BonusTitle)).Select(x => new BonusRelease
                    {
                        Amount = x.Amount - x.Released,
                        BonusId = x.BonusId,
                        NodeId = x.NodeId,
                        Currency = x.Currency
                    }));
                }
            }

            //Now that the unreleased is generated. Create a batchId and release the bonuses.
            if (releases.Count == 0) return null;

            var batchResult = await _paymentureWalletService.CreateBatch(releases);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Factory.StartNew(async () =>
            {
                //get customer details incase we need to create wallet accounts
                string formattedUrlQueryIds = string.Join("&", releases.Select(x => $"ids={x.NodeId}"));
                var customerDetails = await _client.GetValue<CustomerDetails[]>($"{_options.PillarsApiUrl}/api/v1/Customers?{formattedUrlQueryIds}");

                // Process the commission batch
                var processPaymentsResult = await _paymentureWalletService.ProcessCommissionBatch(releases, batchResult, customerDetails);

                //Iterate over releases and set Status based success of the payment on paymenture side
                foreach (var paymentResult in processPaymentsResult)
                {
                    var bonus = releases.Find(x => $"{x.BonusId} | {x.NodeId} | {x.Currency}" == paymentResult.Data);

                    if (bonus != null)
                    {
                        bonus.Status = paymentResult.Status == ResponseStatus.Success ? Status.Success : Status.Failure;
                    }
                }

                var released = await _bonusRepository.ReleaseBonuses(clientId, batchResult.BatchId, releases.ToArray());

                var distinctBonuses = released.Select(x => x.BonusId).Distinct();
                var distinctPeriods = released.Select(x => x.PeriodId).Distinct();

                //call /api/v1/Bonuses/Titles and pass distinctBonuses and take the 'title' to add to the transaction message
                //call /api/v1/CompensationPlans/0/Periods and pass distinctPeriods and take the 'begin' and 'end' dates 
                // take the 'title' and dates to make the transaction message
                string bonusIdsQueryParams = string.Join("&", distinctBonuses.Select(x => $"bonusIds={x}"));
                var bonusTitles = await _client.GetValue<BonusTitles[]>($"{_options.PillarsApiUrl}/api/v1/Bonuses/Titles?{bonusIdsQueryParams}");
                string periodsQueryParams = string.Join("&", distinctPeriods.Select(x => $"IdList={x}"));
                var compPeriods = await _client.GetValue<CompensationPlanPeriod[]>($"{_options.PillarsApiUrl}/api/v1/CompensationPlans/0/Periods?{periodsQueryParams}");

                //TODO: take the titles and period dates and update the paymenture transaction to have a meaningful message

                //take the 'released' values and update the batch payments with the resulting DetailId
                //var result = await _batchRepository.UpdateBatch(clientId, batchResult.BatchId, released.ToArray());
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return new Batch
            {
                Id = batchResult.BatchId,
                Created = DateTime.UtcNow
            };
        }
    }
}
