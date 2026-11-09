using Asp.Versioning;
using ECommerce.Application.Common;
using ECommerce.Application.Marketplaces.Trendyol;
using ECommerce.Marketplaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Marketplaces.Trendyol;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/marketplaces/trendyol/batches")]
[Tags("Trendyol - Toplu işlemler")]
public class TrendyolBatchesController(ITrendyolBatchService batches) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<BatchDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] MarketplaceBatchStatus? status, [FromQuery] int page = 0, [FromQuery] int size = 20, CancellationToken cancellationToken = default) =>
        Ok(await batches.ListAsync(status, page, size, cancellationToken));

    [HttpGet("{batchRequestId}")]
    [ProducesResponseType<BatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string batchRequestId, CancellationToken cancellationToken)
    {
        var batch = await batches.GetAsync(batchRequestId, cancellationToken);
        return batch is null ? Problem(title: "Toplu işlem bulunamadı.", statusCode: StatusCodes.Status404NotFound) : Ok(batch);
    }
}
