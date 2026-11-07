using ECommerce.Application.Common;
using ECommerce.Catalog;
using ECommerce.Core.Persistence;
using ECommerce.Marketplaces;
using ECommerce.Ordering;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace ECommerce.Application.Ordering;

public interface IOrderManagementService
{
    Task<PagedResult<OrderSummaryDto>> ListForCustomerAsync(int customerId, int page, int size, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> GetForCustomerAsync(int customerId, string orderNumber, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> CancelByCustomerAsync(int customerId, string orderNumber, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryDto>> ListAsync(OrderAdminQuery query, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> ConfirmPaymentAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> StartPreparingAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> ShipAsync(int id, ShipOrderRequest request, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> DeliverAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<OrderDto>> CancelAsync(int id, string reason, CancellationToken cancellationToken = default);
}

public class OrderManagementService(
    IRepository<Order> orders,
    IRepository<Product> products,
    IRepository<ProductVariant> variants,
    IRepository<MarketplaceOrderStatusUpdate> statusUpdates,
    IUnitOfWork unitOfWork,
    IOptions<CheckoutOptions> options,
    TimeProvider clock) : IOrderManagementService
{
    private readonly CheckoutOptions _options = options.Value;

    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<OrderSummaryDto>> ListForCustomerAsync(int customerId, int page, int size, CancellationToken cancellationToken = default) =>
        await PageAsync(q => q.Where(o => o.CustomerId == customerId), page, size, cancellationToken);

    public async Task<Result<OrderDto>> GetForCustomerAsync(int customerId, string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await orders.FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderNumber == orderNumber, cancellationToken);
        return order is null ? NotFound() : Result<OrderDto>.Success(order.ToDto(_options));
    }

    public async Task<Result<OrderDto>> CancelByCustomerAsync(int customerId, string orderNumber, CancellationToken cancellationToken = default)
    {
        var order = await orders.FirstOrDefaultAsync(o => o.CustomerId == customerId && o.OrderNumber == orderNumber, cancellationToken);
        if (order is null)
            return NotFound();
        if (!order.CanCustomerCancel)
            return Result<OrderDto>.Invalid("Bu sipariş artık iptal edilemiyor; lütfen bizimle iletişime geçin.");

        return await CancelCoreAsync(order, "Müşteri tarafından iptal edildi.", cancellationToken);
    }

    public async Task<PagedResult<OrderSummaryDto>> ListAsync(OrderAdminQuery query, CancellationToken cancellationToken = default)
    {
        var term = query.Search?.Trim().ToLower();
        return await PageAsync(q => q
            .Where(o => query.Status == null || o.Status == query.Status)
            .Where(o => query.Channel == null || o.Channel == query.Channel)
            .Where(o => term == null || o.OrderNumber.ToLower().Contains(term) || o.CustomerEmail.Contains(term) || o.CustomerName.ToLower().Contains(term)),
            query.Page, query.Size, cancellationToken);
    }

    public async Task<Result<OrderDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await FindAsync(id, cancellationToken);
        return order is null ? NotFound() : Result<OrderDto>.Success(order.ToDto(_options));
    }

    public Task<Result<OrderDto>> ConfirmPaymentAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, o => o.ConfirmPayment(Now), cancellationToken);

    public Task<Result<OrderDto>> StartPreparingAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, o =>
        {
            o.StartPreparing(Now);
            NotifyMarketplace(o, MarketplaceOrderAction.Picking);
        }, cancellationToken);

    public Task<Result<OrderDto>> ShipAsync(int id, ShipOrderRequest request, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, o =>
        {
            o.Ship(request.CargoProvider, request.TrackingNumber, Now, request.InvoiceNumber);
            NotifyMarketplace(o, MarketplaceOrderAction.Invoiced);
        }, cancellationToken);

    // Pazaryeri siparişindeki yerel ilerleme aynı kayıtta outbox'a yazılır; işçi pazaryerine bildirir.
    private void NotifyMarketplace(Order order, MarketplaceOrderAction action)
    {
        if (MarketplaceNames.ForChannel(order.Channel) is { } marketplace)
            statusUpdates.Add(MarketplaceOrderStatusUpdate.For(order, action, marketplace));
    }

    public Task<Result<OrderDto>> DeliverAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeAsync(id, o => o.Deliver(Now), cancellationToken);

    public async Task<Result<OrderDto>> CancelAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        var order = await FindAsync(id, cancellationToken);
        return order is null ? NotFound() : await CancelCoreAsync(order, reason, cancellationToken);
    }

    private async Task<Result<OrderDto>> CancelCoreAsync(Order order, string reason, CancellationToken cancellationToken)
    {
        order.Cancel(reason, Now);

        // İptal edilen web siparişinin stoğu geri eklenir; silinmiş varyantlar atlanır.
        if (order.Channel == OrderChannel.Web)
        {
            var quantities = order.Lines.Where(l => l.VariantId is not null).GroupBy(l => l.VariantId!.Value).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
            var productIds = await variants.ListAsync(q => q.Where(v => quantities.Keys.Contains(v.Id)).Select(v => v.ProductId), cancellationToken);
            foreach (var product in await products.ListAsync(p => productIds.Contains(p.Id), cancellationToken))
                foreach (var variant in product.Variants.Where(v => quantities.ContainsKey(v.Id)))
                    variant.IncreaseStock(quantities[variant.Id]);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<OrderDto>.Success(order.ToDto(_options));
    }

    private async Task<Result<OrderDto>> ChangeAsync(int id, Action<Order> change, CancellationToken cancellationToken)
    {
        var order = await FindAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        change(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<OrderDto>.Success(order.ToDto(_options));
    }

    private async Task<PagedResult<OrderSummaryDto>> PageAsync(Func<IQueryable<Order>, IQueryable<Order>> filter, int page, int size, CancellationToken cancellationToken)
    {
        size = Math.Clamp(size, 1, 100);
        page = Math.Max(page, 0);
        var total = await orders.CountAsync(q => filter(q), cancellationToken);
        var items = await orders.ListAsync(q => filter(q).OrderByDescending(o => o.PlacedAt).ThenByDescending(o => o.Id).Skip(page * size).Take(size), cancellationToken);
        return new PagedResult<OrderSummaryDto>(page, size, total, items.Select(o => o.ToSummary()).ToList());
    }

    private Task<Order?> FindAsync(int id, CancellationToken cancellationToken) => orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    private static Result<OrderDto> NotFound() => Result<OrderDto>.NotFound("Sipariş bulunamadı.");
}

public class ShipOrderRequestValidator : AbstractValidator<ShipOrderRequest>
{
    public ShipOrderRequestValidator()
    {
        RuleFor(x => x.CargoProvider).MaximumLength(100);
        RuleFor(x => x.TrackingNumber).MaximumLength(100);
        RuleFor(x => x.InvoiceNumber).MaximumLength(50);
    }
}

public class CancelOrderRequestValidator : AbstractValidator<CancelOrderRequest>
{
    public CancelOrderRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}
