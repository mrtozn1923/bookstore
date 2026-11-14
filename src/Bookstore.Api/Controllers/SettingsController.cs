using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Bookstore.Api.Options;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IOptions<CatalogOptions> _options;
    private readonly IOptionsSnapshot<CatalogOptions> _snapshot;
    private readonly IOptionsMonitor<CatalogOptions> _monitor;

    public SettingsController(
        IOptions<CatalogOptions> options,
        IOptionsSnapshot<CatalogOptions> snapshot,
        IOptionsMonitor<CatalogOptions> monitor)
    {
        _options = options;
        _snapshot = snapshot;
        _monitor = monitor;
    }

    [HttpGet]
    public IActionResult Get()
    {
        CatalogOptions current = _snapshot.Value;
        return Ok(new
        {
            current.PageSize,
            current.Currency,
            current.SupportEmail,
            // We never return the secret itself, only whether it exists
            HasPricingApiKey = current.PricingApiKey.Length > 0
        });
    }

    [HttpGet("compare")]
    public IActionResult Compare()
    {
        return Ok(new
        {
            options = _options.Value.PageSize,
            snapshot = _snapshot.Value.PageSize,
            monitor = _monitor.CurrentValue.PageSize
        });
    }
}
