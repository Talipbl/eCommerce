using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AdminPanel.Controllers;

[Route("brands")]
public class BrandsController(IBrandsClient brands) : PanelController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        ViewData["Search"] = search;
        var list = await brands.ListAsync(string.IsNullOrWhiteSpace(search) ? null : search.Trim(), cancellationToken);
        return View(list.OrderBy(b => b.Name, Display.TurkishOrder).ToList());
    }

    [HttpGet("new")]
    public IActionResult Create() => View("Form", new BrandFormModel());

    [HttpPost("new")]
    public async Task<IActionResult> Create(BrandFormModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("Form", model);

        try
        {
            var created = await brands.CreateAsync(model.ToRequest(), cancellationToken);
            FlashSuccess($"{created.Name} markası eklendi.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception);
            return View("Form", model);
        }
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        // API tekil marka ucu sunmuyor; marka listesi küçük olduğu için listeden bulunur.
        var brand = (await brands.ListAsync(null, cancellationToken)).FirstOrDefault(b => b.Id == id);
        return brand is null ? NotFound() : View("Form", BrandFormModel.From(brand));
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, BrandFormModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid)
            return View("Form", model);

        try
        {
            var updated = await brands.UpdateAsync(id, model.ToRequest(), cancellationToken);
            FlashSuccess($"{updated.Name} markası güncellendi.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception);
            return View("Form", model);
        }
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await brands.DeleteAsync(id, cancellationToken);
            FlashSuccess("Marka silindi.");
        }
        catch (ApiException exception)
        {
            FlashError(exception);
        }

        return RedirectToAction(nameof(Index));
    }
}
