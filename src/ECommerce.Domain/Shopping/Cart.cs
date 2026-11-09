using ECommerce.Core.Domain;

namespace ECommerce.Shopping;

/// <summary>
/// Müşteri sepeti. Fiyat saklanmaz; satırlar her okumada güncel varyant fiyatı ve stoğuyla değerlendirilir.
/// </summary>
public class Cart : AggregateRoot<int>
{
    public const int MaxQuantityPerLine = 20;
    public const int MaxLines = 50;

    private readonly List<CartItem> _items = [];

    public int CustomerId { get; private set; }

    public IReadOnlyList<CartItem> Items => _items;

    public static Cart Create(int customerId) => new() { CustomerId = customerId };

    public CartItem AddItem(int variantId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.VariantId == variantId);
        if (item is null)
        {
            if (_items.Count >= MaxLines)
                throw new DomainException($"Sepete en fazla {MaxLines} farklı ürün eklenebilir.");
            item = new CartItem(variantId);
            _items.Add(item);
        }

        item.Quantity = ValidQuantity(item.Quantity + quantity);
        return item;
    }

    public void SetQuantity(int variantId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.VariantId == variantId)
            ?? throw new DomainException("Ürün sepette bulunamadı.");

        if (quantity == 0)
            _items.Remove(item);
        else
            item.Quantity = ValidQuantity(quantity);
    }

    public void Remove(int variantId) => _items.RemoveAll(i => i.VariantId == variantId);

    public void Clear() => _items.Clear();

    private static int ValidQuantity(int quantity) =>
        quantity is >= 1 and <= MaxQuantityPerLine
            ? quantity
            : throw new DomainException($"Adet 1 ile {MaxQuantityPerLine} arasında olmalıdır.");
}

public class CartItem : AggregateRoot<int>
{
    private CartItem()
    {
    }

    internal CartItem(int variantId) => VariantId = variantId;

    public int CartId { get; private set; }

    public int VariantId { get; private set; }

    public int Quantity { get; internal set; }
}
