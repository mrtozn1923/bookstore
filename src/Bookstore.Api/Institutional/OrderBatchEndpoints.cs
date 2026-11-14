using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Bookstore.Api.Catalog;
using Bookstore.Api.Common;
using Bookstore.Api.Identity;
using Bookstore.Api.Ordering;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Institutional;

public sealed record OrderBatchLine(
    [property: Required(ErrorMessage = "required"), RegularExpression("^97[89][0-9]{10}$", ErrorMessage = "invalid-format")]
    string Isbn,
    [property: Range(1, 500, ErrorMessage = "out-of-range")] int Quantity);

public sealed record OrderBatchRequest(
    [property: Required(ErrorMessage = "required")] string UnitId,
    [property: Required(ErrorMessage = "required"), Length(1, 500, ErrorMessage = "out-of-range")]
    IReadOnlyList<OrderBatchLine> Lines,
    bool AllOrNothing = false);

public enum LineStatus { Accepted, Rejected }

// One result per request line, in request order (line numbers start at 1).
public sealed record OrderBatchLineResult(int Line, string Isbn, LineStatus Status, ProblemDetails? Problem = null);

public sealed record OrderBatchReport(
    Guid Id,
    string InstitutionId,
    string UnitId,
    Guid? OrderId,
    int AcceptedLines,
    int RejectedLines,
    IReadOnlyList<OrderBatchLineResult> Results);

public sealed class OrderBatchData
{
    private readonly ConcurrentDictionary<Guid, OrderBatchReport> _batches = new();

    public void Add(OrderBatchReport report) => _batches[report.Id] = report;

    public OrderBatchReport? Find(string institutionId, Guid id) =>
        _batches.TryGetValue(id, out OrderBatchReport? report) && report.InstitutionId == institutionId ? report : null;
}

public static class OrderBatchEndpoints
{
    public const string Kind = "order-batch";
    private const decimal InstitutionalDiscount = 0.10m;

    public static IEndpointRouteBuilder MapOrderBatchEndpoints(this IEndpointRouteBuilder app, QuotaPolicy batchQuota)
    {
        RouteGroupBuilder batches = app.MapGroup("/institutions/{institutionId}/order-batches").RequireAuthorization();
        // Quota first, then Idempotency-Key: a retry counts against the quota too.
        batches.MapPost("", CreateBatch).WithName("createOrderBatch")
            .AddEndpointFilter(batchQuota.InvokeAsync)
            .AddEndpointFilter((context, next) => context.HttpContext.RequestServices
                .GetRequiredService<IdempotencyStore>().InvokeAsync<OrderBatchRequest>(context, next));
        batches.MapGet("/{batchId:guid}", GetBatch).WithName("getOrderBatch");
        return app;
    }

    private static async Task<IResult> CreateBatch(
        string institutionId, OrderBatchRequest request, HttpContext http, ClaimsPrincipal principal,
        InstitutionData institutions, CatalogData catalog, OrderData orders, OrderBatchData batches,
        OperationStore operations)
    {
        DemoUser user = principal.Current();
        if (institutions.Find(institutionId) is not { } institution ||
            (user.InstitutionId != institutionId && user.Role != "StoreStaff"))
        {
            return ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/institutions/{institutionId}"]);
        }

        if (institution.Units.All(u => u.Id != request.UnitId))
        {
            return Paging.Invalid("unitId", "invalid-value");
        }

        // The unit is checked once for the whole batch; lines cannot name another unit.
        if (user.Role == "InstitutionBuyer" && request.UnitId != user.UnitId)
        {
            return ProblemCatalog.Create(ProblemCatalog.UnitNotAllowed, [user.UnitId]);
        }

        // Prefer: respond-async (RFC 7240) turns the same request into a long-running operation.
        if (http.Request.Headers["Prefer"].ToString().Contains("respond-async", StringComparison.OrdinalIgnoreCase))
        {
            Operation operation = operations.Start(Kind, user.Name, null, async (progress, cancellationToken) =>
            {
                OrderBatchReport report = await ProcessAsync(institutionId, request, catalog, orders, progress, cancellationToken);
                batches.Add(report);
                return $"/institutions/{institutionId}/order-batches/{report.Id}";
            });
            http.Response.Headers["Preference-Applied"] = "respond-async";
            return OperationStore.Accepted(http, operation);
        }

        OrderBatchReport result = await ProcessAsync(institutionId, request, catalog, orders, _ => { }, http.RequestAborted);
        if (result.OrderId is null)
        {
            return ProblemCatalog.Create(ProblemCatalog.BatchRejected, [result.RejectedLines, request.Lines.Count],
                new Dictionary<string, object?> { ["results"] = result.Results });
        }

        batches.Add(result);
        return TypedResults.Created($"/institutions/{institutionId}/order-batches/{result.Id}", result);
    }

    private static Results<Ok<OrderBatchReport>, ProblemHttpResult> GetBatch(
        string institutionId, Guid batchId, ClaimsPrincipal principal, OrderBatchData batches)
    {
        DemoUser user = principal.Current();
        return (user.InstitutionId == institutionId || user.Role == "StoreStaff") &&
            batches.Find(institutionId, batchId) is { } report
            ? TypedResults.Ok(report)
            : ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/institutions/{institutionId}/order-batches/{batchId}"]);
    }

    // Business rules are checked per line; schema errors were already rejected as a whole (400).
    private static async Task<OrderBatchReport> ProcessAsync(
        string institutionId, OrderBatchRequest request, CatalogData catalog, OrderData orders, Action<int> progress,
        CancellationToken cancellationToken)
    {
        List<OrderBatchLineResult> results = [];
        List<OrderItem> items = [];
        HashSet<string> seen = [];
        for (int i = 0; i < request.Lines.Count; i++)
        {
            OrderBatchLine line = request.Lines[i];
            ProblemDetails? problem =
                !seen.Add(line.Isbn) ? LineProblem(ProblemCatalog.ValidationFailed, [], new() { ["errors"] = new Dictionary<string, string[]> { ["isbn"] = ["duplicate"] } })
                : catalog.Find(line.Isbn) is not { } book ? LineProblem(ProblemCatalog.NotFound, [$"/books/{line.Isbn}"])
                : line.Quantity > book.StockQuantity
                    ? LineProblem(ProblemCatalog.InsufficientStock, [book.StockQuantity, book.Title], new() { ["availableQuantity"] = book.StockQuantity })
                    : null;
            if (problem is null)
            {
                Book found = catalog.Find(line.Isbn)!;
                items.Add(new OrderItem(found.Isbn, found.Title, line.Quantity, decimal.Round(found.Price * (1 - InstitutionalDiscount), 2)));
            }

            results.Add(new OrderBatchLineResult(i + 1, line.Isbn,
                problem is null ? LineStatus.Accepted : LineStatus.Rejected, problem));

            // Stands in for reserving stock in the warehouse system, line by line.
            await Task.Delay(TimeSpan.FromMilliseconds(2), cancellationToken);
            if ((i + 1) % 100 == 0)
            {
                progress((i + 1) * 100 / request.Lines.Count);
            }
        }

        int rejected = results.Count(r => r.Status == LineStatus.Rejected);
        Guid? orderId = items.Count > 0 && !(request.AllOrNothing && rejected > 0)
            ? orders.AddInstitutional(institutionId, request.UnitId, items).Id
            : null;
        return new OrderBatchReport(Guid.CreateVersion7(), institutionId, request.UnitId, orderId, items.Count, rejected, results);
    }

    private static ProblemDetails LineProblem(ProblemType type, object?[] args, Dictionary<string, object?>? extensions = null)
    {
        ProblemDetails problem = new()
        {
            Type = type.Type, Title = ProblemCatalog.Title(type), Status = type.Status, Detail = ProblemCatalog.Detail(type, args),
        };
        foreach ((string key, object? value) in extensions ?? [])
        {
            problem.Extensions[key] = value;
        }

        return problem;
    }
}
