using ECommerce.Application.Common;

namespace ECommerce.Application.Shopping;

public interface ICartService
{
    Task<CartDto> GetAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Result<CartDto>> AddItemAsync(int customerId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<CartDto>> UpdateItemAsync(int customerId, int variantId, int quantity, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(int customerId, int variantId, CancellationToken cancellationToken = default);
}
