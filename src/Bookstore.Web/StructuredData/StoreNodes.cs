using System.Text.Json.Serialization;

namespace Bookstore.Web.StructuredData;

public sealed record ContactPointNode(
    [property: JsonPropertyName("contactType")] string ContactType,
    [property: JsonPropertyName("email")] string Email)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "ContactPoint";
}

public sealed record PostalAddressNode(
    [property: JsonPropertyName("addressLocality")] string AddressLocality,
    [property: JsonPropertyName("addressCountry")] string AddressCountry)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "PostalAddress";
}

public sealed record MerchantReturnPolicyNode(
    string Id,
    [property: JsonPropertyName("applicableCountry")] IReadOnlyList<string> ApplicableCountry,
    [property: JsonPropertyName("returnPolicyCountry")] string ReturnPolicyCountry,
    [property: JsonPropertyName("returnPolicyCategory")] string ReturnPolicyCategory,
    [property: JsonPropertyName("merchantReturnDays")] int MerchantReturnDays,
    [property: JsonPropertyName("returnMethod")] string ReturnMethod,
    [property: JsonPropertyName("returnFees")] string ReturnFees,
    [property: JsonPropertyName("refundType")] string RefundType)
    : SchemaNode("MerchantReturnPolicy", Id);

public sealed record DefinedRegionNode([property: JsonPropertyName("addressCountry")] string AddressCountry)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "DefinedRegion";
}

public sealed record MonetaryAmountNode(
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("value")] decimal? Value = null,
    [property: JsonPropertyName("minValue")] decimal? MinValue = null,
    [property: JsonPropertyName("maxValue")] decimal? MaxValue = null)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "MonetaryAmount";
}

public sealed record DaysNode(
    [property: JsonPropertyName("minValue")] int MinValue,
    [property: JsonPropertyName("maxValue")] int MaxValue)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "QuantitativeValue";

    [JsonPropertyName("unitCode")]
    public string UnitCode => "DAY";
}

public sealed record ServicePeriodNode(
    [property: JsonPropertyName("duration")] DaysNode Duration,
    [property: JsonPropertyName("businessDays")] IReadOnlyList<string> BusinessDays,
    [property: JsonPropertyName("cutoffTime")] string? CutoffTime = null)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "ServicePeriod";
}

public sealed record ShippingConditionsNode(
    [property: JsonPropertyName("shippingDestination")] DefinedRegionNode ShippingDestination,
    [property: JsonPropertyName("orderValue")] MonetaryAmountNode OrderValue,
    [property: JsonPropertyName("shippingRate")] MonetaryAmountNode ShippingRate,
    [property: JsonPropertyName("transitTime")] ServicePeriodNode TransitTime)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "ShippingConditions";
}

public sealed record ShippingServiceNode(
    string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("fulfillmentType")] string FulfillmentType,
    [property: JsonPropertyName("handlingTime")] ServicePeriodNode HandlingTime,
    [property: JsonPropertyName("shippingConditions")] IReadOnlyList<ShippingConditionsNode> ShippingConditions)
    : SchemaNode("ShippingService", Id);

// One node per site: minimal on every page, complete on the policy page
public sealed record OnlineStoreNode(
    string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("logo")] string Logo,
    [property: JsonPropertyName("sameAs")] IReadOnlyList<string> SameAs,
    [property: JsonPropertyName("contactPoint")] ContactPointNode? ContactPoint = null,
    [property: JsonPropertyName("address")] PostalAddressNode? Address = null,
    [property: JsonPropertyName("hasMerchantReturnPolicy")] IReadOnlyList<MerchantReturnPolicyNode>? HasMerchantReturnPolicy = null,
    [property: JsonPropertyName("hasShippingService")] IReadOnlyList<ShippingServiceNode>? HasShippingService = null)
    : SchemaNode("OnlineStore", Id);

// The offer points at the store-wide policies by @id instead of repeating them
public sealed record OfferShippingDetailsNode([property: JsonPropertyName("hasShippingService")] IdRef HasShippingService)
{
    [JsonPropertyName("@type"), JsonPropertyOrder(-1)]
    public string Type => "OfferShippingDetails";
}
