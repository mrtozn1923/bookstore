namespace Bookstore.Api.Institutional;

public class OrderAudit(IHttpContextAccessor accessor, ILogger<OrderAudit> logger)
{
    public void Record(string orderNumber)
    {
        // Accessor yalnız request sürerken dolu bir HttpContext verir
        HttpContext context = accessor.HttpContext
            ?? throw new InvalidOperationException("OrderAudit must be used inside a request.");

        logger.LogInformation("Order {OrderNumber} placed by unit {UnitId} (trace {TraceId})",
            orderNumber, TenantMiddleware.GetUnit(context).Id, context.TraceIdentifier);
    }
}
