namespace Bookstore.Web.Seo;

public sealed class SiteOptions
{
    // canonical URL'lerin kökü: asla Host header'ından türetilmez (Teknik SEO 05)
    public string BaseUrl { get; set; } = "https://bookstore.example";

    // görsellerin kökü: production'da CDN, geliştirmede boş (aynı uygulamadan servis edilir)
    public string ImageBaseUrl { get; set; } = "";

    public string AbsoluteImageBase => string.IsNullOrEmpty(ImageBaseUrl) ? BaseUrl : ImageBaseUrl;
}
