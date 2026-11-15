using System.Runtime.Serialization;
using CoreWCF;

namespace Bookstore.FakeEInvoice;

// The integrator's own contract: its names and its namespace, not Bookstore's.
[ServiceContract(Namespace = IntegratorNamespace.Value)]
public interface IEInvoiceService
{
    [OperationContract]
    [FaultContract(typeof(IntegratorFault))]
    Task<InvoiceReceipt> SendInvoice(InvoiceDocument request);

    [OperationContract]
    [FaultContract(typeof(IntegratorFault))]
    Task<InvoiceStatusInfo> GetInvoiceStatus(string uuid);
}

public static class IntegratorNamespace
{
    public const string Value = "http://einvoice.example.com/integrator/v1";
}

[DataContract(Namespace = IntegratorNamespace.Value)]
public sealed class InvoiceDocument
{
    // ETTN: the invoice's universally unique id, chosen by the sender.
    [DataMember(Order = 1, IsRequired = true)] public string Uuid { get; set; } = "";
    [DataMember(Order = 2, IsRequired = true)] public string InvoiceNumber { get; set; } = "";
    [DataMember(Order = 3, IsRequired = true)] public DateTime IssueDate { get; set; }
    [DataMember(Order = 4, IsRequired = true)] public string SenderTaxNumber { get; set; } = "";
    [DataMember(Order = 5, IsRequired = true)] public string ReceiverTaxNumber { get; set; } = "";
    [DataMember(Order = 6, IsRequired = true)] public string ReceiverName { get; set; } = "";
    [DataMember(Order = 7, IsRequired = true)] public decimal PayableAmount { get; set; }
    [DataMember(Order = 8, IsRequired = true)] public string CurrencyCode { get; set; } = "TRY";
    [DataMember(Order = 9)] public InvoiceLine[] Lines { get; set; } = [];
}

[DataContract(Namespace = IntegratorNamespace.Value)]
public sealed class InvoiceLine
{
    [DataMember(Order = 1)] public string ItemName { get; set; } = "";
    [DataMember(Order = 2)] public int Quantity { get; set; }
    [DataMember(Order = 3)] public decimal UnitPrice { get; set; }
}

[DataContract(Namespace = IntegratorNamespace.Value)]
public sealed class InvoiceReceipt
{
    [DataMember(Order = 1)] public string Uuid { get; set; } = "";
    [DataMember(Order = 2)] public string EnvelopeId { get; set; } = "";
    [DataMember(Order = 3)] public string Status { get; set; } = "";
    [DataMember(Order = 4)] public DateTime ReceivedAt { get; set; }
}

[DataContract(Namespace = IntegratorNamespace.Value)]
public sealed class InvoiceStatusInfo
{
    [DataMember(Order = 1)] public string Uuid { get; set; } = "";
    [DataMember(Order = 2)] public string EnvelopeId { get; set; } = "";
    [DataMember(Order = 3)] public string Status { get; set; } = "";
}

[DataContract(Namespace = IntegratorNamespace.Value)]
public sealed class IntegratorFault
{
    [DataMember(Order = 1)] public string Code { get; set; } = "";
    [DataMember(Order = 2)] public string Message { get; set; } = "";
}
