using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Bookstore.IO.Xml.Shipping;

// The shipping partner expects this exact XML shape.
[XmlRoot("ShipmentNotice", Namespace = "urn:bookstore:shipping")]
[DataContract(Name = "ShipmentNotice", Namespace = "urn:bookstore:shipping")]
public sealed class ShipmentNotice
{
    [XmlAttribute("orderNumber")]
    [DataMember(Order = 1)]
    public string OrderNumber { get; set; } = "";

    [XmlElement("Carrier")]
    [DataMember(Order = 2)]
    public string Carrier { get; set; } = "";

    [XmlElement("TrackingCode")]
    [DataMember(Order = 3)]
    public string TrackingCode { get; set; } = "";

    [XmlArray("Parcels"), XmlArrayItem("Parcel")]
    [DataMember(Order = 4)]
    public List<Parcel> Parcels { get; set; } = [];

    [XmlIgnore]
    public decimal InternalCost { get; set; }
}

[DataContract(Namespace = "urn:bookstore:shipping")]
public sealed class Parcel
{
    [XmlAttribute("weightKg")]
    [DataMember(Order = 1)]
    public decimal WeightKg { get; set; }

    [XmlText]
    [DataMember(Order = 2)]
    public string Isbn { get; set; } = "";
}
