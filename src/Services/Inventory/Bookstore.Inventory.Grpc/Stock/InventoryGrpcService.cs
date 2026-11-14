using System.Diagnostics;
using Bookstore.Inventory.Data;
using Bookstore.Inventory.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Inventory.Stock;

public sealed class InventoryGrpcService(InventoryDbContext db, StockReservations reservations)
    : InventoryService.InventoryServiceBase
{
    public override async Task<ReserveStockResponse> ReserveStock(ReserveStockRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.OrderId, out Guid orderId) || request.Lines.Count == 0 || request.Lines.Any(l => l.Quantity < 1))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "order_id must be a GUID and every line needs a positive quantity."));
        }

        ReservationOutcome outcome;
        try
        {
            // context.CancellationToken fires at the caller's deadline: the database work stops with it.
            outcome = await reservations.ReserveAsync(
                orderId, [.. request.Lines.Select(l => new StockRequest(l.BookId, l.Quantity))], context.CancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RpcException(new Status(StatusCode.Aborted, "Stock is changing too fast; retry the reservation."));
        }

        return outcome switch
        {
            ReservationOutcome.Reserved reserved => new ReserveStockResponse
            {
                ReservationId = reserved.Reservation.Id.ToString(),
                ExpiresAt = Timestamp.FromDateTime(DateTime.SpecifyKind(reserved.Reservation.ExpiresAtUtc, DateTimeKind.Utc))
            },
            ReservationOutcome.Unavailable unavailable => throw new RpcException(
                new Status(StatusCode.FailedPrecondition, $"Not enough stock for: {string.Join(", ", unavailable.Isbns)}."),
                new Metadata { { "unavailable-books", string.Join(",", unavailable.Isbns) } }),
            _ => throw new UnreachableException()
        };
    }

    public override async Task<ReleaseStockResponse> ReleaseStock(ReleaseStockRequest request, ServerCallContext context)
    {
        IQueryable<StockReservation> query = request.TargetCase switch
        {
            ReleaseStockRequest.TargetOneofCase.ReservationId when Guid.TryParse(request.ReservationId, out Guid id) =>
                db.Reservations.Where(r => r.Id == id),
            ReleaseStockRequest.TargetOneofCase.OrderId when Guid.TryParse(request.OrderId, out Guid orderId) =>
                db.Reservations.Where(r => r.OrderId == orderId),
            _ => throw new RpcException(new Status(StatusCode.InvalidArgument, "Give a reservation_id or an order_id as a GUID."))
        };

        StockReservation reservation = await query.FirstOrDefaultAsync(context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "Reservation not found."));

        int released = await reservations.ReleaseAsync(reservation, ReservationStatus.Released, context.CancellationToken);
        return new ReleaseStockResponse { ReleasedLines = released };
    }

    public override async Task<GetStockResponse> GetStock(GetStockRequest request, ServerCallContext context)
    {
        StockItem item = await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Isbn == request.BookId, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"No stock record for {request.BookId}."));

        return new GetStockResponse
        {
            BookId = item.Isbn,
            Available = item.Available,
            ReservedQuantity = item.QuantityReserved,
            Status = item.Available switch
            {
                <= 0 => StockStatus.OutOfStock,
                <= 3 => StockStatus.Low,
                _ => StockStatus.InStock
            },
            UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(item.UpdatedAtUtc, DateTimeKind.Utc))
        };
    }
}
