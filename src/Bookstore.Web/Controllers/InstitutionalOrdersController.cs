using Bookstore.Web.Catalog;
using Bookstore.Web.Institutional;
using Bookstore.Web.State;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers;

[Route("institutional-orders/new")]
public sealed class InstitutionalOrdersController(IBookCatalog catalog, InstitutionalOrderService orders) : Controller
{
    private const string DraftKey = "InstitutionalOrder.Draft";

    [HttpGet("institution")]
    public async Task<IActionResult> Institution(CancellationToken cancellationToken)
    {
        InstitutionalOrderDraft draft = await LoadDraftAsync(cancellationToken);
        return View(new InstitutionStep
        {
            InstitutionName = draft.InstitutionName ?? "",
            UnitName = draft.UnitName,
            ContactEmail = draft.ContactEmail ?? ""
        });
    }

    [HttpPost("institution")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Institution(InstitutionStep step, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(step);
        }

        InstitutionalOrderDraft draft = await LoadDraftAsync(cancellationToken);
        draft.InstitutionName = step.InstitutionName;
        draft.UnitName = step.UnitName;
        draft.ContactEmail = step.ContactEmail;
        HttpContext.Session.SetJson(DraftKey, draft);
        return RedirectToAction(nameof(Books));
    }

    [HttpGet("books")]
    public async Task<IActionResult> Books(CancellationToken cancellationToken)
    {
        InstitutionalOrderDraft draft = await LoadDraftAsync(cancellationToken);
        if (draft.InstitutionName is null)
        {
            return RedirectToAction(nameof(Institution)); // step 1 is missing (or the session is gone)
        }

        return View(new BooksStep { Quantities = draft.Quantities, Catalog = await catalog.ListAsync(null, cancellationToken) });
    }

    [HttpPost("books")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Books(BooksStep step, CancellationToken cancellationToken)
    {
        InstitutionalOrderDraft draft = await LoadDraftAsync(cancellationToken);
        if (draft.InstitutionName is null)
        {
            return RedirectToAction(nameof(Institution));
        }

        draft.Quantities = step.Quantities.Where(q => q.Value > 0).ToDictionary();
        if (draft.TotalCopies < InstitutionalOrderService.MinimumCopies)
        {
            ModelState.AddModelError("", $"Institutional orders need at least {InstitutionalOrderService.MinimumCopies} copies (now {draft.TotalCopies}).");
            step.Catalog = await catalog.ListAsync(null, cancellationToken);
            return View(step);
        }

        HttpContext.Session.SetJson(DraftKey, draft);
        return RedirectToAction(nameof(Review));
    }

    [HttpGet("review")]
    public async Task<IActionResult> Review(CancellationToken cancellationToken)
    {
        InstitutionalOrderDraft draft = await LoadDraftAsync(cancellationToken);
        if (draft.InstitutionName is null || draft.TotalCopies < InstitutionalOrderService.MinimumCopies)
        {
            return RedirectToAction(nameof(Institution));
        }

        return View(new ReviewStep(draft, await orders.SummarizeAsync(draft, cancellationToken)));
    }

    [HttpPost("review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(CancellationToken cancellationToken)
    {
        InstitutionalOrderDraft draft = await LoadDraftAsync(cancellationToken);
        if (draft.InstitutionName is null || draft.TotalCopies < InstitutionalOrderService.MinimumCopies)
        {
            return RedirectToAction(nameof(Institution));
        }

        string number = orders.Submit(draft);
        HttpContext.Session.Remove(DraftKey);
        TempData["OrderNumber"] = number;
        return RedirectToAction(nameof(Done));
    }

    [HttpGet("done")]
    public IActionResult Done() => View(nameof(Done), TempData["OrderNumber"] as string);

    private async Task<InstitutionalOrderDraft> LoadDraftAsync(CancellationToken cancellationToken)
    {
        // Load the session asynchronously; otherwise the first access loads it synchronously
        await HttpContext.Session.LoadAsync(cancellationToken);
        return HttpContext.Session.GetJson<InstitutionalOrderDraft>(DraftKey) ?? new InstitutionalOrderDraft();
    }
}
