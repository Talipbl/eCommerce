using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ECommerce.AdminPanel.Controllers;

public class HomeController(IDashboardClient dashboard) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            return View(new DashboardPage(await dashboard.SummaryAsync(cancellationToken), null));
        }
        catch (ApiException exception)
        {
            return View(new DashboardPage(null, ApiProblem.From(exception).Describe()));
        }
    }

    [AllowAnonymous]
    [Route("error/{statusCode:int?}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        var status = statusCode ?? StatusCodes.Status500InternalServerError;
        Response.StatusCode = status;
        return View(new ErrorViewModel(status, Activity.Current?.Id ?? HttpContext.TraceIdentifier));
    }
}
