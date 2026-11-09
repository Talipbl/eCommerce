using ECommerce.Marketplaces;
using Mapster;
using Trendyol.Client.Catalog;

namespace ECommerce.Application.Marketplaces.Trendyol;

public class TrendyolMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<MarketplaceBatchRequest, BatchDto>()
            .Map(d => d.CreatedAt, s => s.CreatedDate);
        config.NewConfig<MarketplaceCategoryMapping, CategoryMappingDto>();
        config.NewConfig<MarketplaceBrandMapping, BrandMappingDto>();

        config.NewConfig<TrendyolBrand, TrendyolBrandDto>();
        config.NewConfig<TrendyolAttributeValue, AttributeValueDto>()
            .Map(d => d.Id, s => s.AttributeValueId)
            .Map(d => d.Value, s => s.AttributeValue);
        config.NewConfig<TrendyolCategoryAttribute, CategoryAttributeDto>()
            .Map(d => d.Id, s => s.Attribute.Id)
            .Map(d => d.Name, s => s.Attribute.Name)
            .Map(d => d.AllowMultipleValues, s => s.AllowMultipleAttributeValues);
    }
}
