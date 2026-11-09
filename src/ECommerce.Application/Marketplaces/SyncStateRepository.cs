using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;

namespace ECommerce.Application.Marketplaces;

internal static class SyncStateRepository
{
    public static async Task<MarketplaceSyncState> GetOrCreateAsync(this IRepository<MarketplaceSyncState> states, string marketplace, string job, CancellationToken cancellationToken)
    {
        var state = await states.FirstOrDefaultAsync(s => s.Marketplace == marketplace && s.Job == job, cancellationToken);
        if (state is null)
            states.Add(state = MarketplaceSyncState.Create(marketplace, job));
        return state;
    }
}
