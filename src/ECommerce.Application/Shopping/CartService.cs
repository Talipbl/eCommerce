using ECommerce.Application.Catalog;
using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Shopping;
using FluentValidation;

namespace ECommerce.Application.Shopping;

public class CartService(
    IRepository<Cart> carts,
    IRepository<Product> products,
    IRepository<ProductVariant> variants,
    IUnitOfWork unitOfWork,
    IMediaUrlBuilder media) : ICartService
{
    public async Task<CartDto> GetAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var cart = await carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        return await ToDtoAsync(cart, cancellationToken);
    }

    public async Task<Result<CartDto>> AddItemAsync(int customerId, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var cart = await carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        if (cart is null)
        {
            cart = Cart.Create(customerId);
            carts.Add(cart);
        }

        var current = cart.Items.FirstOrDefault(i => i.VariantId == request.VariantId)?.Quantity ?? 0;
        var error = await CheckAvailabilityAsync(request.VariantId, current + request.Quantity, cancellationToken);
        if (error is not null)
            return error;

        cart.AddItem(request.VariantId, request.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CartDto>.Success(await ToDtoAsync(cart, cancellationToken));
    }

    public async Task<Result<CartDto>> UpdateItemAsync(int customerId, int variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var cart = await carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        if (cart is null || cart.Items.All(i => i.VariantId != variantId))
            return Result<CartDto>.NotFound("Ürün sepette bulunamadı.");

        if (quantity > 0)
        {
            var error = await CheckAvailabilityAsync(variantId, quantity, cancellationToken);
            if (error is not null)
                return error;
        }

        cart.SetQuantity(variantId, quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CartDto>.Success(await ToDtoAsync(cart, cancellationToken));
    }

    public async Task<CartDto> RemoveItemAsync(int customerId, int variantId, CancellationToken cancellationToken = default)
    {
        var cart = await carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        if (cart is not null)
        {
            cart.Remove(variantId);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return await ToDtoAsync(cart, cancellationToken);
    }

    public static async Task<Dictionary<int, PurchasableVariant>> LoadVariantsAsync(
        IRepository<ProductVariant> variants, IRepository<Product> products, IReadOnlyCollection<int> variantIds, CancellationToken cancellationToken)
    {
        var productIds = await variants.ListAsync(q => q.Where(v => variantIds.Contains(v.Id)).Select(v => v.ProductId), cancellationToken);
        var loaded = await products.ListAsync(p => productIds.Contains(p.Id), cancellationToken);
        return loaded
            .SelectMany(p => p.Variants.Where(v => variantIds.Contains(v.Id)).Select(v => new PurchasableVariant(p, v)))
            .ToDictionary(x => x.Variant.Id);
    }

    private async Task<AppError?> CheckAvailabilityAsync(int variantId, int quantity, CancellationToken cancellationToken)
    {
        var found = await LoadVariantsAsync(variants, products, [variantId], cancellationToken);
        if (!found.TryGetValue(variantId, out var item) || !item.IsPurchasable)
            return Errors.Invalid("Ürün satışta değil.");
        if (item.Variant.StockQuantity < quantity)
            return Errors.Invalid($"Stokta yalnızca {item.Variant.StockQuantity} adet var.");
        return null;
    }

    private async Task<CartDto> ToDtoAsync(Cart? cart, CancellationToken cancellationToken)
    {
        if (cart is null || cart.Items.Count == 0)
            return new CartDto([], 0, 0, false);

        var found = await LoadVariantsAsync(variants, products, cart.Items.Select(i => i.VariantId).ToList(), cancellationToken);
        var items = cart.Items.Select(i =>
        {
            if (!found.TryGetValue(i.VariantId, out var pv))
                return new CartItemDto(i.VariantId, 0, "Ürün artık satışta değil", string.Empty, [], 0, null, i.Quantity, 0, 0, false, null);

            var available = pv.IsPurchasable && pv.Variant.StockQuantity >= i.Quantity;
            var image = pv.Product.Images.OrderBy(img => img.SortOrder).FirstOrDefault();
            return new CartItemDto(i.VariantId, pv.Product.Id, pv.Product.Name, pv.Product.Slug,
                pv.Variant.Attributes.Select(a => new VariantAttributeDto(a.Name, a.Value)).ToList(),
                pv.Variant.Price, pv.Variant.ListPrice, i.Quantity, pv.Variant.Price * i.Quantity,
                pv.IsPurchasable ? pv.Variant.StockQuantity : 0, available,
                image is null ? null : media.ToPublicUrl(image.Path));
        }).ToList();

        return new CartDto(items, items.Where(i => i.IsAvailable).Sum(i => i.LineTotal), items.Sum(i => i.Quantity), items.Any(i => !i.IsAvailable));
    }
}
