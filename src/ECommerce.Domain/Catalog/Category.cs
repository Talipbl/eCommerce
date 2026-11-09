using ECommerce.Core.Domain;

namespace ECommerce.Catalog;

public class Category : AggregateRoot<int>
{
    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string? Description { get; private set; }

    public int? ParentId { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Category Create(string name, string slug, int? parentId, string? description, int sortOrder)
    {
        var category = new Category { ParentId = parentId };
        category.Update(name, slug, description, sortOrder);
        return category;
    }

    public void Update(string name, string slug, string? description, int sortOrder)
    {
        Name = CatalogRules.RequiredText(name, "Kategori adı", 100);
        Slug = CatalogRules.ValidSlug(slug);
        Description = description?.Trim();
        SortOrder = sortOrder;
    }

    public void MoveTo(int? parentId)
    {
        if (parentId is not null && parentId == Id)
            throw new DomainException("Kategori kendi alt kategorisi olamaz.");
        ParentId = parentId;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
