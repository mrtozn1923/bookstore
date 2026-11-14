namespace Bookstore.Institutional.Domain;

public enum OrderStatus { Draft, PendingApprover, PendingPurchasingManager, Approved, Rejected, Cancelled }

public enum ApprovalStep { Approver, PurchasingManager }

public enum ApprovalDecision { Approved, Rejected }

public enum ShipmentPreference { SingleShipment, PartialAllowed }
