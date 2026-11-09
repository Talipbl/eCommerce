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
    public async Task<IActionResult> AddItem(AddCartItemRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await cart.AddItemAsync(User.GetAccountId(), request, cancellationToken), Ok);

    [HttpPut("items/{variantId:int}")]
    public async Task<IActionResult> UpdateItem(int variantId, UpdateCartItemRequest request, CancellationToken cancellationToken) =>
        this.ToActionResult(await cart.UpdateItemAsync(User.GetAccountId(), variantId, request.Quantity, cancellationToken), Ok);

    [HttpDelete("items/{variantId:int}")]
    public async Task<CartDto> RemoveItem(int variantId, CancellationToken cancellationToken) =>
        await cart.RemoveItemAsync(User.GetAccountId(), variantId, cancellationToken);
}
