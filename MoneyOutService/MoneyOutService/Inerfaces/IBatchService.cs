using MoneyOutService.Models;

namespace MoneyOutService.Inerfaces
{
    public interface IBatchService
    {
        public Task<BatchSummaryCount> GetBatches(int clientId, DateTime? begin, DateTime? end, int offset, int count);
        public Task<Batch?> CreateBatch(int clientId, NewBatch batch);
        public Task<Batch?> GetBatch(int clientId, long batchId);
    }
}
