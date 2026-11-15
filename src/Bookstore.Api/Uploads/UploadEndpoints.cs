using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Uploads;

public static class UploadEndpoints
{
    public static void MapUploads(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder uploads = app.MapGroup("/uploads").RequireAuthorization("Publisher");

        uploads.MapPost("/", (CreateUploadRequest request, ClaimsPrincipal user, UploadStore store, UploadSasIssuer sas,
            IOptions<UploadOptions> options, ILoggerFactory loggers) =>
        {
            // The declared length is only a hint; the real size is checked after the upload
            if (request.Length <= 0 || request.Length > options.Value.MaxBytes(request.Purpose))
            {
                return Results.Problem($"Length must be between 1 and {options.Value.MaxBytes(request.Purpose):N0} bytes.", statusCode: 413);
            }

            UploadRecord record = store.Add(new UploadRecord
            {
                Id = Guid.NewGuid(),
                PublisherId = user.FindFirstValue("publisher_id")!,
                Isbn = request.Isbn,
                Purpose = request.Purpose,
                DisplayName = UploadFileName.ForDisplay(request.FileName),
            });
            loggers.CreateLogger("Bookstore.Api.Uploads").LogInformation("Upload {UploadId} created for {DisplayName}", record.Id, record.DisplayName);
            return Results.Ok(new CreateUploadResponse(record.Id, sas.ForUpload(record)));
        });

        uploads.MapPost("/{id:guid}/complete", async (Guid id, ClaimsPrincipal user, UploadStore store, UploadQueue queue, CancellationToken ct) =>
        {
            if (Find(store, id, user) is not { Status: UploadStatus.AwaitingUpload } record)
            {
                return Results.NotFound();
            }

            record.Status = UploadStatus.Processing;
            await queue.EnqueueAsync(id, ct);
            return Results.Accepted($"/uploads/{id}", new UploadStatusResponse(id, record.Status, null));
        });

        uploads.MapGet("/{id:guid}", (Guid id, ClaimsPrincipal user, UploadStore store) =>
            Find(store, id, user) is { } record
                ? Results.Ok(new UploadStatusResponse(id, record.Status, record.Reason))
                : Results.NotFound());

        uploads.MapGet("/{id:guid}/download", (Guid id, ClaimsPrincipal user, UploadStore store, UploadSasIssuer sas) =>
            Find(store, id, user) switch
            {
                null => Results.NotFound(),
                { Status: UploadStatus.Approved } record => Results.Redirect(sas.ForDownload(record).ToString()),
                var record => Results.Conflict(new UploadStatusResponse(id, record.Status, null)),
            });
    }

    // Someone else's upload looks exactly like a missing one
    private static UploadRecord? Find(UploadStore store, Guid id, ClaimsPrincipal user) =>
        store.Get(id) is { } record && record.PublisherId == user.FindFirstValue("publisher_id") ? record : null;
}
