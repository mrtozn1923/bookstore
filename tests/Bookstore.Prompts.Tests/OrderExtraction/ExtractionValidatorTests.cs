using Bookstore.Prompts.Tasks.OrderExtraction;

namespace Bookstore.Prompts.Tests.OrderExtraction;

public class ExtractionValidatorTests
{
    static readonly OrderEmail Email = new(new DateOnly(2026, 11, 12),
        "9. sınıflar için 40 adet Dune (ISBN 9780441013593) ve 35 adet Emma istiyoruz.");

    [Fact]
    public void Isbn_copied_from_another_book_is_reported()
    {
        Extraction extraction = new(
            [
                new ExtractedItem("Dune", "9780441013593", 40, "40 adet Dune"),
                new ExtractedItem("Emma", "9780441013593", 35, "35 adet Emma"),
            ], null, null);

        List<string> issues = ExtractionValidator.Validate(extraction, Email);

        Assert.Equal(["Emma: ISBN 9780441013593 is written next to Dune"], issues);
    }

    [Fact]
    public void Quote_with_a_changed_letter_is_reported()
    {
        Extraction extraction = new(
            [new ExtractedItem("Emma", null, 35, "35 adet Emma ıstiyoruz")], null, null);

        List<string> issues = ExtractionValidator.Validate(extraction, Email);

        Assert.Equal(["Emma: source quote is not in the email"], issues);
    }

    [Theory]
    [InlineData("9780441013593", true)]
    [InlineData("9780441013594", false)]
    [InlineData("978044101359", false)]
    public void Checks_isbn13_check_digit(string isbn, bool valid) =>
        Assert.Equal(valid, ExtractionValidator.IsValidIsbn13(isbn));
}
