using ECommerce.Catalog;
using Mapster;

namespace ECommerce.Application.Catalog;

public class CatalogMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryDto>();
        config.NewConfig<Brand, BrandDto>();
        config.NewConfig<VariantAttribute, VariantAttributeDto>();
        config.NewConfig<ProductVariant, VariantDto>();

        // Görsel URL'i medya ayarına bağlı; ProductDetailDto MappingParameters.Media ile eşlenir.
        config.NewConfig<ProductImage, ProductImageDto>()
            .Map(d => d.Url, s => MappingParameters.Get<Common.IMediaUrlBuilder>(MappingParameters.Media).ToPublicUrl(s.Path));
        config.NewConfig<Product, ProductDetailDto>()
            .Map(d => d.Images, s => s.Images.OrderBy(i => i.SortOrder));
    }
}
