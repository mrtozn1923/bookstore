using Microsoft.AspNetCore.Mvc;
using Bookstore.Api.Services;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LifetimesController : ControllerBase
{
    private readonly SingletonOperation _singleton;
    private readonly ScopedOperation _scoped;
    private readonly TransientOperation _transient;
    private readonly OperationReporter _reporter;

    public LifetimesController(
        SingletonOperation singleton,
        ScopedOperation scoped,
        TransientOperation transient,
        OperationReporter reporter)
    {
        _singleton = singleton;
        _scoped = scoped;
        _transient = transient;
        _reporter = reporter;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            controller = new { singleton = _singleton.Id, scoped = _scoped.Id, transient = _transient.Id },
            reporter = _reporter.Report()
        });
    }

    [HttpGet("reporter")]
    public IActionResult FromAction([FromServices] OperationReporter reporter)
    {
        return Ok(reporter.Report());
    }
}
