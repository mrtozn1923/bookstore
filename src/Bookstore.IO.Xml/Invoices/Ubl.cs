using System.Xml.Linq;

namespace Bookstore.IO.Xml.Invoices;

// UBL 2.1 namespaces. The element set we use is a simplified subset.
public static class Ubl
{
    public static readonly XNamespace Invoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    public static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    public static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
}
