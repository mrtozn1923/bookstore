namespace Bookstore.Institutional.StateMachines;

// Veritabanına yazılan durumlar: yalnız yaprak state'ler
public enum OrderStatus { Draft, PendingApprover, PendingPurchasingManager, Approved, Rejected, Cancelled }

public enum OrderTrigger { Submit, Approve, Reject, Cancel }
