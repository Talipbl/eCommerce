using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests.Infrastructure;

[ApiController]
[AllowAnonymous]
[ApiVersionNeutral]
[Route("test/throw")]
public class ThrowingTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => throw new InvalidOperationException("Test hatası");
}
