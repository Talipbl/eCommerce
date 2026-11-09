using ECommerce.Core.Domain;

namespace ECommerce.Catalog;

public class Brand : AggregateRoot<int>
{
    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public bool IsActive { get; private set; } = true;

    public static Brand Create(string name, string slug)
    {
        var brand = new Brand();
        brand.Update(name, slug);
        return brand;
    }

    public void Update(string name, string slug)
    {
        Name = CatalogRules.RequiredText(name, "Marka adı", 100);
        Slug = CatalogRules.ValidSlug(slug);
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
