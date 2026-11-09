using ECommerce.Catalog;
using ECommerce.Core.Domain;

namespace ECommerce.UnitTests.Catalog;

public class CategoryTests
{
    [Fact]
    public void Category_cannot_be_its_own_parent()
    {
        var category = Category.Create("Giyim", "giyim", parentId: null, description: null, sortOrder: 0);
        category.Id = 5;

        Should.Throw<DomainException>(() => category.MoveTo(5));
    }

    [Fact]
    public void Invalid_slug_is_rejected() =>
        Should.Throw<DomainException>(() => Category.Create("Giyim", "Giyim Kadın", null, null, 0));

    [Fact]
    public void Brand_requires_name() =>
        Should.Throw<DomainException>(() => Brand.Create(" ", "marka"));
}
