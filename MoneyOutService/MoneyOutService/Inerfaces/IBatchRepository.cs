using MoneyOutService.Models;

namespace MoneyOutService.Inerfaces
{
    public interface IBatchRepository
    {
        public Task<BatchSummaryCount> GetBatches(int clientId, DateTime? begin, DateTime? end, int offset, int count);
        public Task<long> CreateBatchId(int clientId);
        public Task<Batch> UpdateBatch(int clientId, long batchId, ReleaseResult[] releases);
        public Task<Batch> GetBatch(int clientId, long batchId);
    }
}
