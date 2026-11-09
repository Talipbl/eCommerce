using ECommerce.Application.Common;
using ECommerce.Application.Shopping;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Customers;
using ECommerce.Ordering;
using ECommerce.Shopping;
using Microsoft.Extensions.Options;

namespace ECommerce.Application.Ordering;

/// <summary>
/// Sepetten sipariş oluşturur: stok düşme, sipariş kaydı ve sepetin temizlenmesi tek kayıtta yapılır.
/// </summary>
public class CheckoutService(
    IRepository<Cart> carts,
    IRepository<Customer> customers,
    IRepository<Product> products,
    IRepository<ProductVariant> variants,
    IRepository<Order> orders,
    IUnitOfWork unitOfWork,
    IOptions<CheckoutOptions> options,
    TimeProvider clock) : ICheckoutService
{
    private readonly CheckoutOptions _options = options.Value;

    public async Task<Result<CheckoutPreviewDto>> PreviewAsync(int customerId, PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        var prepared = await PrepareLinesAsync(customerId, cancellationToken);
        if (prepared.Error is not null)
            return prepared.Error;

        var subtotal = prepared.Lines.Sum(l => l.Input.UnitPrice * l.Input.Quantity);
        return Result<CheckoutPreviewDto>.Success(Totals(subtotal, paymentMethod));
    }

    public async Task<Result<OrderDto>> PlaceOrderAsync(int customerId, CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PaymentMethod == PaymentMethod.Marketplace)
            return Result<OrderDto>.Invalid("Geçersiz ödeme yöntemi.");

        var customer = await customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        if (customer is null)
            return Result<OrderDto>.NotFound("Müşteri bulunamadı.");

        var address = customer.Addresses.FirstOrDefault(a => a.Id == request.AddressId);
        if (address is null)
            return Result<OrderDto>.Invalid("Teslimat adresi bulunamadı.");

        var prepared = await PrepareLinesAsync(customerId, cancellationToken);
        if (prepared.Error is not null)
            return prepared.Error;

        var now = clock.GetUtcNow().UtcDateTime;
        var subtotal = prepared.Lines.Sum(l => l.Input.UnitPrice * l.Input.Quantity);
        var totals = Totals(subtotal, request.PaymentMethod);

        foreach (var line in prepared.Lines)
            line.Variant.DecreaseStock(line.Input.Quantity);

        var order = Order.PlaceWebOrder(
            OrderNumbers.New(now), customer.Id, customer.Email, $"{customer.FirstName} {customer.LastName}",
            new OrderAddress(address.FullName, address.Phone, address.City, address.District, address.AddressLine, address.PostalCode),
            request.PaymentMethod, prepared.Lines.Select(l => l.Input).ToList(), totals.ShippingFee, totals.PaymentFee, request.Note, now);

        orders.Add(order);
        prepared.Cart!.Clear();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<OrderDto>.Conflict("Sepetinizdeki ürünlerin stoğu bu sırada değişti; lütfen sepeti kontrol edip tekrar deneyin.");
        }

        return Result<OrderDto>.Success(order.ToDto(_options));
    }

    private CheckoutPreviewDto Totals(decimal subtotal, PaymentMethod method)
    {
        var shipping = _options.ShippingFeeFor(subtotal);
        var payment = _options.PaymentFeeFor(method);
        return new CheckoutPreviewDto(subtotal, shipping, payment, subtotal + shipping + payment);
    }

    private async Task<(Cart? Cart, List<(OrderLineInput Input, ProductVariant Variant)> Lines, AppError? Error)> PrepareLinesAsync(
        int customerId, CancellationToken cancellationToken)
    {
        var cart = await carts.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
            return (cart, [], Errors.Invalid("Sepetiniz boş."));

        var found = await CartService.LoadVariantsAsync(variants, products, cart.Items.Select(i => i.VariantId).ToList(), cancellationToken);
        var lines = new List<(OrderLineInput, ProductVariant)>();
        var problems = new List<string>();

        foreach (var item in cart.Items)
        {
            if (!found.TryGetValue(item.VariantId, out var pv) || !pv.IsPurchasable)
            {
                problems.Add(pv?.Product.Name ?? "Satıştan kaldırılan ürün");
                continue;
            }
            if (pv.Variant.StockQuantity < item.Quantity)
            {
                problems.Add($"{pv.Product.Name} (stokta {pv.Variant.StockQuantity} adet)");
                continue;
            }

            var description = pv.Variant.Attributes.Count == 0 ? null : string.Join(", ", pv.Variant.Attributes.Select(a => $"{a.Name}: {a.Value}"));
            lines.Add((new OrderLineInput(pv.Variant.Id, pv.Product.Id, pv.Product.Name, description, pv.Variant.Barcode, pv.Variant.Sku,
                pv.Variant.Price, item.Quantity, pv.Product.VatRate), pv.Variant));
        }

        return problems.Count > 0
            ? (cart, lines, Errors.Invalid("Şu ürünler sipariş edilemiyor: " + string.Join("; ", problems)))
            : (cart, lines, null);
    }
}
