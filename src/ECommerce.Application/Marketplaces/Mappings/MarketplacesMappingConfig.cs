using ECommerce.Marketplaces;
using Mapster;

namespace ECommerce.Application.Marketplaces;

public class MarketplacesMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<MarketplaceSyncLog, SyncLogDto>();
        config.NewConfig<MarketplaceSyncState, SyncStateDto>();
        config.NewConfig<MarketplaceListing, ListingDto>();
    }
}
