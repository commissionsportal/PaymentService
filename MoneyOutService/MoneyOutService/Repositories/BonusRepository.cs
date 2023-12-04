using MoneyOutService.Inerfaces;
using MoneyOutService.Models;

namespace MoneyOutService.Repositories
{
    public class BonusRepository : IBonusRepository
    {
        private readonly IClient _client;

        public BonusRepository(IClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<UnreleasedBonus>> GetUnreleasedBonuses(int clientId, DateTime? date, string[]? nodeIds, int offset, int count)
        {
            var nodePart = nodeIds != null && nodeIds.Length > 0 ? "&nodeIds=" + string.Join("&nodeIds=", nodeIds) : string.Empty;
            var datePart = date.HasValue ? $"&date={date.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture)}" : string.Empty;

            var result = await _client.GetValue<UnreleasedBonus[]>($"/api/v1/{clientId}/Bonuses/Unreleased?offset={offset}&count={count}{datePart}{nodePart}");
            return result;
        }

        public async Task<IEnumerable<ReleaseResult>> ReleaseBonuses(int clientId, long batchId, BonusRelease[] releases)
        {
            return await _client.Put<ReleaseResult[], BonusRelease[]>($"/api/v1/{clientId}/Bonuses/Release", releases);
        }
    }
}
