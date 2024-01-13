using MoneyOutService.Models;

namespace MoneyOutService.Interfaces
{
    public interface IBonusRepository
    {
        Task<IEnumerable<UnreleasedBonus>> GetUnreleasedBonuses(int clientId, DateTime? date, string[]? nodeIds, int offset, int count);
        Task<IEnumerable<ReleaseResult>> ReleaseBonuses(int clientId, string batchId, BonusRelease[] releases);
    }
}
