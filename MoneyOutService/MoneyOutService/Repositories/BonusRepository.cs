using Microsoft.Extensions.Options;
using MoneyOutService.Interfaces;
using MoneyOutService.Models;
using MoneyOutService.Options;

namespace MoneyOutService.Repositories
{
    public class BonusRepository : IBonusRepository
    {
        private readonly IClient _client;
        private readonly PaymentureMoneyOutServiceOptions _options;

        public BonusRepository(IClient client, IOptions<PaymentureMoneyOutServiceOptions> options)
        {
            _client = client;
            _options = options.Value;
        }

        public async Task<IEnumerable<UnreleasedBonus>> GetUnreleasedBonuses(int clientId, DateTime? date, string[]? nodeIds, int offset, int count)
        {
            var nodePart = nodeIds != null && nodeIds.Length > 0 ? "&nodeIds=" + string.Join("&nodeIds=", nodeIds) : string.Empty;
            var datePart = date.HasValue ? $"&date={date.Value.ToString("o", System.Globalization.CultureInfo.InvariantCulture)}" : string.Empty;

            var result = await _client.GetValue<UnreleasedBonus[]>($"{_options.PillarsApiUrl}/api/v1/Bonuses/Unreleased?offset={offset}&count={count}{datePart}{nodePart}");
            return result;
        }

        public async Task<IEnumerable<ReleaseResult>> ReleaseBonuses(int clientId, string batchId, BonusRelease[] releases)
        {
            return await _client.Put<ReleaseResult[], BonusRelease[]>($"{_options.PillarsApiUrl}/api/v1/{clientId}/Bonuses/Release", releases);
        }
    }
}