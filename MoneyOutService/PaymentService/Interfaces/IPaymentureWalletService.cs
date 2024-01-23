using MoneyOutService.Models;
using MoneyOutService.Models.PaymentureWallet;

namespace MoneyOutService.Interfaces
{
    public interface IPaymentureWalletService
    {
        Task<BatchResult> CreateBatch(List<BonusRelease> releases);
        Task<List<StringResponse>> ProcessCommissionBatch(List<BonusRelease> releases, BatchResult batchInfo, CustomerDetails[] customerDetails);
    }
}