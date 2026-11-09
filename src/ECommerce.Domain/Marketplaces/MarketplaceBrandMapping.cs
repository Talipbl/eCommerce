using ECommerce.Core.Domain;

namespace ECommerce.Marketplaces;

public class MarketplaceBrandMapping : AggregateRoot<int>
{
    public string Marketplace { get; private set; } = null!;

    public int BrandId { get; private set; }

    public long ExternalBrandId { get; private set; }

    public string? ExternalBrandName { get; private set; }

    public static MarketplaceBrandMapping Create(string marketplace, int brandId, long externalBrandId, string? externalBrandName) =>
        new() { Marketplace = marketplace, BrandId = brandId, ExternalBrandId = externalBrandId, ExternalBrandName = externalBrandName };

    public void Update(long externalBrandId, string? externalBrandName)
    {
        ExternalBrandId = externalBrandId;
        ExternalBrandName = externalBrandName;
    }
}
