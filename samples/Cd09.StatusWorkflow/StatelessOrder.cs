using Stateless;

namespace Bookstore.Institutional.Workflow;

public static class StatelessOrder
{
    public static StateMachine<OrderStatus, string> Create(Order order, UserInfo user)
    {
        StateMachine<OrderStatus, string> m = new(() => order.Status, s => order.Status = s);
        bool Is(string role) => user.Roles.Contains(role);

        m.Configure(OrderStatus.Draft)
            .PermitIf("Submit", OrderStatus.PendingApproval, () => Is("InstitutionBuyer"))
            .PermitIf("Cancel", OrderStatus.Cancelled, () => Is("InstitutionBuyer"));
        m.Configure(OrderStatus.PendingApproval)
            .PermitIf("Approve", OrderStatus.Approved, () => Is("InstitutionApprover"))
            .PermitIf("Reject", OrderStatus.Rejected, () => Is("InstitutionApprover"));
        m.Configure(OrderStatus.Rejected)
            .PermitIf("Revise", OrderStatus.Draft, () => Is("InstitutionBuyer"));
        m.Configure(OrderStatus.Approved)
            .PermitIf("Ship", OrderStatus.Shipped, () => Is("WarehouseStaff"))
            .PermitIf("Cancel", OrderStatus.Cancelled, () => Is("InstitutionApprover"));
        return m;
    }
}
