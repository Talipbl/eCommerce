using Asp.Versioning;
using ECommerce.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Dashboard;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/dashboard")]
[Tags("Dashboard")]
public class DashboardController(IDashboardService dashboard) : ControllerBase
{
    /// <summary>Sipariş durumları, onay bekleyen havaleler, bugün / son 7 gün ciro, düşük stok ve senkron uyarıları.</summary>
    [HttpGet("summary")]
    public async Task<DashboardSummaryDto> Summary(CancellationToken cancellationToken) =>
        await dashboard.GetSummaryAsync(cancellationToken);
}
