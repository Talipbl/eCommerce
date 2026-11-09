using ECommerce.Application.Common;

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
