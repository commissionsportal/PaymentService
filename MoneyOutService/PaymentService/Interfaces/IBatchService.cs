using MoneyOutService.Models;

namespace MoneyOutService.Interfaces
{
    public interface IBatchService
    {
        public Task<Batch?> CreateBatch(int clientId, NewBatch batch);
        //public Task<Batch?> GetBatch(int clientId, long batchId);
    }
}
