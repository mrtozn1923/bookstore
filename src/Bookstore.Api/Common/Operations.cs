using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Common;

// Status values as in the Azure guidelines' status monitor.
public enum OperationStatus { NotStarted, Running, Succeeded, Failed, Canceled }

// The status monitor of a long-running operation (GET /operations/{operationId}).
public sealed record OperationResponse(
    Guid Id,
    string Kind,
    OperationStatus Status,
    int PercentComplete,
    DateTime CreatedAt,
    DateTime LastUpdatedAt,
    string? ResourceLocation,
    ProblemDetails? Error);

// Thrown by a job to end its operation as Failed with a catalog problem.
public sealed class OperationFailedException(ProblemType type, params object?[] args) : Exception(type.Code)
{
    public ProblemType Type { get; } = type;
    public object?[] Args { get; } = args;
}

// Reports progress; returns the location of the resource the operation produced.
public delegate Task<string?> OperationJob(Action<int> progress, CancellationToken cancellationToken);

public sealed class Operation
{
    public required Guid Id { get; init; }
    public required string Kind { get; init; }
    public required string Owner { get; init; }
    public required string? Subject { get; init; }
    public required OperationJob Job { get; init; }
    public required DateTime CreatedAt { get; init; }
    public CancellationTokenSource Cancellation { get; } = new();
    public OperationStatus Status { get; set; } = OperationStatus.NotStarted;
    public int PercentComplete { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResourceLocation { get; set; }
    public ProblemDetails? Error { get; set; }

    public bool IsTerminal => Status is OperationStatus.Succeeded or OperationStatus.Failed or OperationStatus.Canceled;

    public OperationResponse ToResponse() =>
        new(Id, Kind, Status, PercentComplete, CreatedAt, LastUpdatedAt, ResourceLocation, Error);
}

public sealed class OperationStore(TimeProvider time)
{
    // Status monitors are kept for 24 hours after the operation ends (documented in the contract).
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);
    public const int RetryAfterSeconds = 1;

    private readonly ConcurrentDictionary<Guid, Operation> _operations = new();
    private readonly Channel<Operation> _queue = Channel.CreateUnbounded<Operation>();

    public ChannelReader<Operation> Queue => _queue.Reader;

    public Operation Start(string kind, string owner, string? subject, OperationJob job)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        Operation operation = new()
        {
            Id = Guid.CreateVersion7(), Kind = kind, Owner = owner, Subject = subject, Job = job, CreatedAt = now,
            LastUpdatedAt = now,
        };
        _operations[operation.Id] = operation;
        _queue.Writer.TryWrite(operation);
        return operation;
    }

    public Operation? Find(Guid id, string owner) =>
        _operations.TryGetValue(id, out Operation? operation) && operation.Owner == owner &&
        (operation.CompletedAt is null || operation.CompletedAt + Retention > time.GetUtcNow().UtcDateTime)
            ? operation
            : null;

    public Operation? FindActive(string kind, string subject) =>
        _operations.Values.FirstOrDefault(o =>
            o.Kind == kind && o.Subject == subject && o.Status is not (OperationStatus.Failed or OperationStatus.Canceled));

    public void Update(Operation operation, Action<Operation> change)
    {
        lock (operation)
        {
            if (operation.IsTerminal)
            {
                return;
            }

            change(operation);
            operation.LastUpdatedAt = time.GetUtcNow().UtcDateTime;
            if (operation.IsTerminal)
            {
                operation.CompletedAt = operation.LastUpdatedAt;
            }
        }
    }

    // 202 Accepted with the status monitor in the body and its URL in Operation-Location (Azure guidelines).
    public static IResult Accepted(HttpContext http, Operation operation)
    {
        http.Response.Headers["Operation-Location"] = $"/operations/{operation.Id}";
        http.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();
        return TypedResults.Json(operation.ToResponse(), statusCode: StatusCodes.Status202Accepted);
    }
}

// Runs queued operations one by one in the background.
public sealed class OperationWorker(OperationStore store, ILogger<OperationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Operation operation in store.Queue.ReadAllAsync(stoppingToken))
        {
            if (operation.IsTerminal)
            {
                continue;
            }

            store.Update(operation, o => o.Status = OperationStatus.Running);
            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, operation.Cancellation.Token);
            try
            {
                string? location = await operation.Job(p => store.Update(operation, o => o.PercentComplete = p), linked.Token);
                store.Update(operation, o =>
                {
                    o.Status = OperationStatus.Succeeded;
                    o.PercentComplete = 100;
                    o.ResourceLocation = location;
                });
            }
            catch (OperationCanceledException) when (operation.Cancellation.IsCancellationRequested)
            {
                store.Update(operation, o => o.Status = OperationStatus.Canceled);
            }
            catch (OperationFailedException failed)
            {
                store.Update(operation, o =>
                {
                    o.Status = OperationStatus.Failed;
                    o.Error = new ProblemDetails
                    {
                        Type = failed.Type.Type, Title = failed.Type.TitleEn, Status = failed.Type.Status,
                        Detail = string.Format(System.Globalization.CultureInfo.InvariantCulture, failed.Type.DetailEn, failed.Args),
                    };
                });
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Operation {OperationId} failed", operation.Id);
                store.Update(operation, o =>
                {
                    o.Status = OperationStatus.Failed;
                    o.Error = new ProblemDetails
                    {
                        Type = ProblemCatalog.InternalError.Type, Title = ProblemCatalog.InternalError.TitleEn, Status = 500,
                    };
                });
            }
        }
    }
}

public static class OperationEndpoints
{
    public static IEndpointRouteBuilder MapOperationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder operations = app.MapGroup("/operations").RequireAuthorization();
        operations.MapGet("/{operationId:guid}", GetOperation).WithName("getOperation");
        operations.MapPost("/{operationId:guid}:cancel", CancelOperation).WithName("cancelOperation");
        return app;
    }

    private static Results<Ok<OperationResponse>, ProblemHttpResult> GetOperation(
        Guid operationId, HttpContext http, OperationStore store)
    {
        if (store.Find(operationId, http.User.Identity!.Name!) is not { } operation)
        {
            return NotFound(operationId);
        }

        if (!operation.IsTerminal)
        {
            http.Response.Headers.RetryAfter = OperationStore.RetryAfterSeconds.ToString();
        }

        return TypedResults.Ok(operation.ToResponse());
    }

    private static Results<Ok<OperationResponse>, ProblemHttpResult> CancelOperation(
        Guid operationId, HttpContext http, OperationStore store)
    {
        if (store.Find(operationId, http.User.Identity!.Name!) is not { } operation)
        {
            return NotFound(operationId);
        }

        if (operation.IsTerminal)
        {
            return ProblemCatalog.Create(ProblemCatalog.OperationNotCancellable, [operationId, operation.Status]);
        }

        store.Update(operation, o => o.Status = OperationStatus.Canceled);
        operation.Cancellation.Cancel();
        return TypedResults.Ok(operation.ToResponse());
    }

    private static ProblemHttpResult NotFound(Guid operationId) =>
        ProblemCatalog.Create(ProblemCatalog.NotFound, [$"/operations/{operationId}"]);
}
