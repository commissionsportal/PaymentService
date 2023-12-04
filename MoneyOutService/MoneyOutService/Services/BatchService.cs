using MoneyOutService.Inerfaces;
using MoneyOutService.Models;

namespace MoneyOutService.Services
{
    public class BatchService : IBatchService
    {
        private readonly IBonusRepository _bonusRepository;
        private readonly IBatchRepository _batchRepository;

        public BatchService(IBonusRepository bonusRepository, IBatchRepository batchRepository)
        {
            _bonusRepository = bonusRepository;
            _batchRepository = batchRepository;
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
            List<BonusRelease> releases = new List<BonusRelease>();

            foreach (var customerid in customerIds)
            {
                var customerSum = bonuses.Where(x => x.NodeId == customerid && bonusHash.Contains(x.BonusTitle)).Sum(x => x.Amount);
                if (customerSum > 0)
                {
                    releases.AddRange(bonuses.Where(x => x.NodeId == customerid && bonusHash.Contains(x.BonusTitle)).Select(x => new BonusRelease
                    {
                        Amount = x.Amount - x.Released,
                        BonusId = x.BonusId,
                        NodeId = x.NodeId
                    }));
                }
            }

            //Now that the unreleased is generated. Create a batchId and release the bonuses.
            if (releases.Count == 0) return null;

            var batchId = await _batchRepository.CreateBatchId(clientId);
            var released = await _bonusRepository.ReleaseBonuses(clientId, batchId, releases.ToArray());

            return await _batchRepository.UpdateBatch(clientId, batchId, released.ToArray());
        }

        public async Task<BatchSummaryCount> GetBatches(int clientId, DateTime? begin, DateTime? end, int offset, int count)
        {
            return await _batchRepository.GetBatches(clientId, begin, end, offset, count);
        }

        public async Task<Batch?> GetBatch(int clientId, long batchId)
        {
            return await _batchRepository.GetBatch(clientId, batchId);
        }
    }
}
