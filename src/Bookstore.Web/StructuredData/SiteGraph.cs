using Bookstore.Web.Catalog;
using Bookstore.Web.Content;
using Bookstore.Web.Reviews;
using Bookstore.Web.Store;

namespace Bookstore.Web.StructuredData;

// Builds stable @id values and the nodes for each page type
public sealed class SiteGraph(string baseUrl)
{
    public string OrganizationId => $"{baseUrl}/#organization";
    public string WebSiteId => $"{baseUrl}/#website";
    public string PersonId(string authorSlug) => $"{baseUrl}/authors/{authorSlug}#person";

    public static string Prefix(string lang) => lang == "en" ? "/en" : "";

    public string PageUrl(string lang, string path) => $"{baseUrl}{Prefix(lang)}{path}";

    public string ReturnPolicyId => $"{baseUrl}/shipping-and-returns#return-policy";
    public string ShippingServiceId => $"{baseUrl}/shipping-and-returns#standard-shipping";

    // Every page gets the same short store node; details live on the policy page only
    public OnlineStoreNode Organization() =>
        new(OrganizationId, "Bookstore", $"{baseUrl}/", $"{baseUrl}/images/logo-512.png",
            ["https://github.com/bookstore-example"]);

    public OnlineStoreNode Store(StorePolicy policy)
    {
        string[] weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];
        var transit = new ServicePeriodNode(new DaysNode(policy.TransitDaysMin, policy.TransitDaysMax), weekdays);
        var region = new DefinedRegionNode(policy.Country);

        var returns = new MerchantReturnPolicyNode(
            Id: ReturnPolicyId,
            ApplicableCountry: [policy.Country],
            ReturnPolicyCountry: policy.Country,
            ReturnPolicyCategory: "https://schema.org/MerchantReturnFiniteReturnWindow",
            MerchantReturnDays: policy.ReturnDays,
            ReturnMethod: "https://schema.org/ReturnByMail",
            ReturnFees: "https://schema.org/FreeReturn",
            RefundType: "https://schema.org/FullRefund");

        var shipping = new ShippingServiceNode(
            Id: ShippingServiceId,
            Name: "Standard shipping",
            FulfillmentType: "https://schema.org/FulfillmentTypeDelivery",
            HandlingTime: new ServicePeriodNode(new DaysNode(0, policy.HandlingDaysMax), weekdays,
                CutoffTime: policy.CutoffTimeWithOffset),
            ShippingConditions:
            [
                // below the threshold: fixed fee; maxValue is just under the free-shipping amount
                new(region, new MonetaryAmountNode(policy.Currency, MaxValue: policy.FreeShippingFrom - 0.01m),
                    new MonetaryAmountNode(policy.Currency, Value: policy.ShippingFee), transit),
                new(region, new MonetaryAmountNode(policy.Currency, MinValue: policy.FreeShippingFrom),
                    new MonetaryAmountNode(policy.Currency, Value: 0m), transit),
            ]);

        return Organization() with
        {
            ContactPoint = new ContactPointNode("customer service", "support@example.com"),
            Address = new PostalAddressNode("Istanbul", policy.Country),
            HasMerchantReturnPolicy = [returns],
            HasShippingService = [shipping],
        };
    }

    public JsonLdDocument StorePolicyPage(StorePolicy policy) => new([Store(policy), WebSite()]);

    public WebSiteNode WebSite() =>
        new(WebSiteId, "Bookstore", $"{baseUrl}/", ["tr", "en"], new IdRef(OrganizationId));

    public PersonNode Person(Author author, string lang) =>
        new(PersonId(author.Slug), author.Name, PageUrl(lang, $"/authors/{author.Slug}"),
            author.JobTitle.Get(lang), new IdRef(OrganizationId), author.SameAs);

    public JsonLdDocument Home() => new([Organization(), WebSite()]);

    public JsonLdDocument Profile(Author author, string lang)
    {
        string url = PageUrl(lang, $"/authors/{author.Slug}");
        return new([new ProfilePageNode($"{url}#profilepage", url, lang, new IdRef(WebSiteId), new IdRef(PersonId(author.Slug))),
                    Person(author, lang), Organization(), WebSite()]);
    }

    public JsonLdDocument Post(Post post, Author author, string lang)
    {
        string url = PageUrl(lang, $"/blog/{post.Slug}");
        var posting = new BlogPostingNode(
            Id: $"{url}#article",
            Headline: post.Title.Get(lang),
            Description: post.Summary.Get(lang),
            Image: [$"{baseUrl}{post.ImagePath}"],
            DatePublished: post.PublishedAt,
            // only a real edit after publishing counts as a modification
            DateModified: post.UpdatedAt > post.PublishedAt ? post.UpdatedAt : null,
            Author: [new IdRef(PersonId(author.Slug))],
            Publisher: new IdRef(OrganizationId),
            MainEntityOfPage: url,
            InLanguage: lang,
            IsPartOf: new IdRef(WebSiteId));

        var breadcrumb = new BreadcrumbListNode($"{url}#breadcrumb",
        [
            new(1, "Blog", PageUrl(lang, "/blog")),
            new(2, post.Category.Get(lang), PageUrl(lang, $"/blog/category/{post.CategorySlug}")),
            new(3, post.Title.Get(lang), null),
        ]);

        return new([posting, breadcrumb, Person(author, lang), Organization(), WebSite()]);
    }

    public string ProductId(BookEdition book) => $"{baseUrl}/books/{book.Slug}#product";

    // Incentivized reviews stay visible with their note, but schema.org has no field for the note
    private static List<BookReview> MarkupReviews(ReviewSection reviews) =>
        [.. reviews.Latest.Where(r => !r.IsIncentivized)];

    public JsonLdDocument Book(BookEdition book, PriceQuote quote, ReviewSection reviews, string lang)
    {
        string url = PageUrl(lang, $"/books/{book.Slug}");
        var offer = new OfferNode(
            Url: url,
            Price: quote.Price,
            PriceCurrency: quote.Currency,
            Availability: book.StockQuantity > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock",
            ItemCondition: "https://schema.org/NewCondition",
            Seller: new IdRef(OrganizationId),
            // a sale exists only while a campaign runs: old price + sale window
            PriceSpecification: quote.StrikethroughPrice is { } regular
                ? new UnitPriceSpecificationNode("https://schema.org/StrikethroughPrice", regular, quote.Currency)
                : null,
            ValidFrom: quote.SaleStartsAt,
            ValidThrough: quote.SaleEndsAt,
            // the store-wide policies apply; the offer only names them
            HasMerchantReturnPolicy: new IdRef(ReturnPolicyId),
            ShippingDetails: new OfferShippingDetailsNode(new IdRef(ShippingServiceId)));

        var product = new BookProductNode(
            Id: ProductId(book),
            Name: $"{book.Title} ({BookTexts.FormatName(book.Format, lang)})",
            Description: book.Description.Get(lang),
            Image: [.. book.ImagePaths.Select(path => $"{baseUrl}{path}")],
            Isbn: book.Isbn,
            Sku: book.Isbn,
            Author: new NamedNode("Person", book.AuthorName),
            BookFormat: $"https://schema.org/{book.Format}",
            NumberOfPages: book.NumberOfPages,
            InLanguage: book.InLanguage,
            Offers: offer,
            // no reviews, no rating: never publish ratingValue 0 or an empty list
            AggregateRating: reviews.Summary is { } summary
                ? new AggregateRatingNode(summary.RatingValue, summary.RatingCount, summary.ReviewCount)
                : null,
            Review: MarkupReviews(reviews) is { Count: > 0 } marked
                ? [.. marked.Select(r => new ReviewNode(
                    new NamedNode("Person", r.AuthorName),
                    r.CreatedAt.ToString("yyyy-MM-dd"),
                    r.Text!,
                    new RatingNode(r.Rating)))]
                : null);

        var breadcrumb = new BreadcrumbListNode($"{url}#breadcrumb",
        [
            new(1, BookTexts.Books(lang), PageUrl(lang, "/books")),
            new(2, BookTexts.CategoryName(book.Category, lang), PageUrl(lang, $"/books/category/{book.Category}")),
            new(3, product.Name, null),
        ]);

        return new([product, breadcrumb, Organization(), WebSite()]);
    }
}
