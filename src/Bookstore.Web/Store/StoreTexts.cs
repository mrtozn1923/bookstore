using Bookstore.Web.Catalog;

namespace Bookstore.Web.Store;

// Visible policy text, generated from the same StorePolicy as the markup
public static class StoreTexts
{
    public static string Title(string lang) => lang == "en" ? "Shipping and returns" : "Teslimat ve iade";

    public static string ShippingFee(StorePolicy p, string lang) => lang == "en"
        ? $"Shipping within Türkiye costs {PricingService.Format(p.ShippingFee, lang)}; orders of {PricingService.Format(p.FreeShippingFrom, lang)} or more ship free."
        : $"Türkiye içi kargo {PricingService.Format(p.ShippingFee, lang)}; {PricingService.Format(p.FreeShippingFrom, lang)} ve üzeri siparişlerde kargo ücretsiz.";

    public static string DeliveryTime(StorePolicy p, string lang) => lang == "en"
        ? $"Orders placed on weekdays before {p.CutoffTime:HH\\:mm} leave the warehouse within {p.HandlingDaysMax} business day and arrive in {p.TransitDaysMin}–{p.TransitDaysMax} business days."
        : $"Hafta içi saat {p.CutoffTime:HH\\:mm}'den önce verilen siparişler en geç {p.HandlingDaysMax} iş günü içinde depodan çıkar ve {p.TransitDaysMin}–{p.TransitDaysMax} iş gününde teslim edilir.";

    public static string ReturnWindow(StorePolicy p, string lang) => lang == "en"
        ? $"You can return a book by mail within {p.ReturnDays} days of delivery; return shipping is free and you get a full refund."
        : $"Kitabı teslim aldığın günden itibaren {p.ReturnDays} gün içinde kargoyla iade edebilirsin; iade kargosu ücretsiz, ödemenin tamamı geri yapılır.";
}
