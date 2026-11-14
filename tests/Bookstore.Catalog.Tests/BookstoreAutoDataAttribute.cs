using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit3;

namespace Bookstore.Catalog.Tests;

public sealed class BookstoreAutoDataAttribute() : AutoDataAttribute(CreateFixture)
{
    internal static IFixture CreateFixture() =>
        new Fixture().Customize(new BookstoreCustomization()).Customize(new AutoNSubstituteCustomization());
}

public sealed class BookstoreInlineAutoDataAttribute(params object[] values)
    : InlineAutoDataAttribute(BookstoreAutoDataAttribute.CreateFixture, values);
