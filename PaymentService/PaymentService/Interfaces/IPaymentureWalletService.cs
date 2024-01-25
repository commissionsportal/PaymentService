using PaymentService.Models;
using PaymentService.Models.PaymentureWallet;

namespace PaymentService.Interfaces
{
    public interface IPaymentureWalletService
    {
        //Task<BatchResult> CreateBatch(List<ReleaseResult> releases);
        Task<List<StringResponse>> ProcessCommissionBatch(Batch batch, CustomerDetails[] customerDetails);
    }
}