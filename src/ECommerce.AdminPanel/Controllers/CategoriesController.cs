using ECommerce.AdminPanel.Api;
using ECommerce.AdminPanel.Infrastructure;
using ECommerce.AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.AdminPanel.Controllers;

[Route("categories")]
public class CategoriesController(ICategoriesClient categories, CatalogLookups lookups) : PanelController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await lookups.CategoriesAsync(cancellationToken));

    [HttpGet("new")]
    public async Task<IActionResult> Create(int? parentId, CancellationToken cancellationToken) =>
        await FormAsync(new CategoryFormModel { ParentId = parentId }, cancellationToken);

    [HttpPost("new")]
    public async Task<IActionResult> Create(CategoryFormModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return await FormAsync(model, cancellationToken);

        try
        {
            var created = await categories.CreateAsync(model.ToRequest(), cancellationToken);
            FlashSuccess($"{created.Name} kategorisi eklendi.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception);
            return await FormAsync(model, cancellationToken);
        }
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await FormAsync(CategoryFormModel.From(await categories.GetAsync(id, cancellationToken)), cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode == StatusCodes.Status404NotFound)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CategoryFormModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid)
            return await FormAsync(model, cancellationToken);

        try
        {
            var updated = await categories.UpdateAsync(id, model.ToRequest(), cancellationToken);
            FlashSuccess($"{updated.Name} kategorisi güncellendi.");
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException exception)
        {
            ModelState.AddApiProblem(exception);
            return await FormAsync(model, cancellationToken);
        }
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await categories.DeleteAsync(id, cancellationToken);
            FlashSuccess("Kategori silindi.");
        }
        catch (ApiException exception)
        {
            FlashError(exception);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormAsync(CategoryFormModel model, CancellationToken cancellationToken)
    {
        var options = await lookups.CategoriesAsync(cancellationToken);
        model.ParentOptions = model.Id is { } id
            ? options.Where(o => o.Id != id && !o.AncestorIds.Contains(id)).ToList()
            : options;
        return View("Form", model);
    }
}
