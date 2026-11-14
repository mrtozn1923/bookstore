#:project ../src/Services/Inventory/Bookstore.Inventory.Contracts/Bookstore.Inventory.Contracts.csproj
#:package Grpc.Net.Client
#:property PublishAot=false

// A small gRPC client for trying Inventory by hand:
//   dotnet run tools/inventory-probe.cs -- <grpc-address> stock <isbn>
//   dotnet run tools/inventory-probe.cs -- <grpc-address> reserve <order-id> <isbn> <quantity>
//   dotnet run tools/inventory-probe.cs -- <grpc-address> release <order-id>
using Bookstore.Inventory.V1;
using Grpc.Core;
using Grpc.Net.Client;

using GrpcChannel channel = GrpcChannel.ForAddress(args[0]);
InventoryService.InventoryServiceClient client = new(channel);

try
{
    switch (args[1])
    {
        case "stock":
            GetStockResponse stock = await client.GetStockAsync(new GetStockRequest { BookId = args[2] });
            Console.WriteLine($"{stock.BookId}: available={stock.Available} reserved={stock.ReservedQuantity} status={stock.Status}");
            break;
        case "reserve":
            ReserveStockResponse reserved = await client.ReserveStockAsync(new ReserveStockRequest
            {
                OrderId = args[2],
                Lines = { new ReservationLine { BookId = args[3], Quantity = int.Parse(args[4]) } }
            });
            Console.WriteLine($"reservation={reserved.ReservationId} expiresAt={reserved.ExpiresAt.ToDateTime():O}");
            break;
        case "release":
            ReleaseStockResponse released = await client.ReleaseStockAsync(new ReleaseStockRequest { OrderId = args[2] });
            Console.WriteLine($"releasedLines={released.ReleasedLines}");
            break;
    }
}
catch (RpcException ex)
{
    Console.WriteLine($"{ex.StatusCode}: {ex.Status.Detail}");
}
