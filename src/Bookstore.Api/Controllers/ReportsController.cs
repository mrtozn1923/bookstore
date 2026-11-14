using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ILogger<ReportsController> logger)
    {
        _logger = logger;
    }

    [HttpGet("threads")]
    public async Task<IActionResult> Threads(int request)
    {
        _logger.LogInformation("Request {Request} before await: thread {ThreadId}",
            request, Environment.CurrentManagedThreadId);
        await Task.Delay(500);
        _logger.LogInformation("Request {Request} after await: thread {ThreadId}",
            request, Environment.CurrentManagedThreadId);
        return Ok();
    }

    [HttpGet("blocking")]
    public IActionResult Blocking()
    {
        // blocks the thread for one second
        Task.Delay(1000).Wait();
        return Ok();
    }

    [HttpGet("non-blocking")]
    public async Task<IActionResult> NonBlocking()
    {
        // releases the thread and resumes one second later
        await Task.Delay(1000);
        return Ok();
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales(CancellationToken cancellationToken)
    {
        for (int step = 1; step <= 10; step++)
        {
            // each step stands in for a heavy one-second query
            await Task.Delay(1000, cancellationToken);
            _logger.LogInformation("Sales report step {Step}/10 done", step);
        }

        return Ok(new { status = "ready" });
    }

    [HttpGet("sales-no-token")]
    public async Task<IActionResult> SalesNoToken()
    {
        for (int step = 1; step <= 10; step++)
        {
            await Task.Delay(1000);
            _logger.LogInformation("Sales report (no token) step {Step}/10 done", step);
        }

        return Ok(new { status = "ready" });
    }
}
