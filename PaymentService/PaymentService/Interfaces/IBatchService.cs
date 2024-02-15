using PaymentService.Models;

namespace PaymentService.Interfaces
{
    public interface IBatchService
    {
        public Task ProcessBatch(Batch batch, HeaderData headerData);
    }
}
