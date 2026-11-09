using Asp.Versioning;
using ECommerce.API.Infrastructure;
using ECommerce.Application.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Shopping;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Policy = SecurityRegistration.CustomerPolicy)]
[Route("api/v{version:apiVersion}/customer/cart")]
[Tags("Müşteri - Sepet")]
public class CartController(ICartService cart) : ControllerBase
{
    [HttpGet]
    public async Task<CartDto> Get(CancellationToken cancellationToken) => await cart.GetAsync(User.GetAccountId(), cancellationToken);

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await cart.AddItemAsync(User.GetAccountId(), request, cancellationToken));

    [HttpPut("items/{variantId:int}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int variantId, UpdateCartItemRequest request, CancellationToken cancellationToken) =>
        this.ToOk(await cart.UpdateItemAsync(User.GetAccountId(), variantId, request.Quantity, cancellationToken));

    [HttpDelete("items/{variantId:int}")]
    public async Task<CartDto> RemoveItem(int variantId, CancellationToken cancellationToken) =>
        await cart.RemoveItemAsync(User.GetAccountId(), variantId, cancellationToken);
}
