using PaymentService.Models;

namespace PaymentService.Interfaces
{
    public interface IBatchService
    {
        public Task ProcessBatch(string clientId, Batch batch, HeaderData headerData);
    }
}
