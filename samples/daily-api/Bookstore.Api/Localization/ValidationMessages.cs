using System.Globalization;
using System.Resources;

namespace Bookstore.Api.Localization;

// Data Annotations'ın ErrorMessageResourceType'ı public static string property'ler ister
public static class ValidationMessages
{
    private static readonly ResourceManager Resources =
        new("Bookstore.Api.Resources.ValidationMessages", typeof(ValidationMessages).Assembly);

    public static string Range => Resources.GetString(nameof(Range), CultureInfo.CurrentUICulture)!;

    public static string Required => Resources.GetString(nameof(Required), CultureInfo.CurrentUICulture)!;
}
