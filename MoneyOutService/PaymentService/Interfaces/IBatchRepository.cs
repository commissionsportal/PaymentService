using MoneyOutService.Models;

namespace MoneyOutService.Interfaces
{
    public interface IBatchRepository
    {
        public Task<long> CreateBatchId(int clientId);
        //public Task<Batch> UpdateBatch(int clientId, string batchId, ReleaseResult[] releases);
    }
}
