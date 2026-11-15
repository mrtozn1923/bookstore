using Bookstore.Prompts.Tasks.Localization;

namespace Bookstore.Prompts.Tests.Localization;

public class LocalizationCheckTests
{
    static readonly Glossary Glossary = new(
        new Dictionary<string, string> { ["Okul Listesi"] = "School List" },
        ["Bookstore", "The Hobbit"]);

    [Fact]
    public void Clean_translation_has_no_issues()
    {
        List<string> issues = TranslationChecker.Check(
            "<p>{customerName}, <b>The Hobbit</b> kitabını Okul Listesi'ne ekleyebilirsiniz.</p>",
            "<p>{customerName}, you can add <b>The Hobbit</b> to your School List.</p>",
            Glossary);

        Assert.Empty(issues);
    }

    [Fact]
    public void Markdown_answer_with_a_new_term_is_caught()
    {
        List<string> issues = TranslationChecker.Check(
            "<p>{customerName}, <b>The Hobbit</b> kitabını Okul Listesi'ne ekleyebilirsiniz.</p>",
            "Here's the translation:\n\n{customerName}, you can add **The Hobbit** to your Wish List.",
            Glossary);

        Assert.Equal(
            ["text before the HTML: \"Here's the translation: {customerName...\"",
             "HTML tags changed",
             "glossary: \"Okul Listesi\" should be \"School List\""],
            issues);
    }

    [Fact]
    public void Same_number_in_another_format_is_not_new_but_is_flagged_as_turkish()
    {
        Assert.Empty(TextFacts.NewNumbers("1.240,50 TL", "TRY 1,240.50"));
        Assert.Equal(["1.240,50"], TextFacts.TurkishNumbers("on sale: 1.240,50 TL"));
    }

    [Fact]
    public void Campaign_check_counts_characters_and_required_facts()
    {
        Campaign campaign = new("Tüm klasiklerde %25 indirim, KITAP25 kodu 30 Kasım'a kadar.", ["%25", "KITAP25", "30 Kasım"]);

        List<string> issues = CampaignRewrite.Check(campaign, "Klasiklerde %35 indirim, KITAP25 kodunu kullanın!");

        Assert.Equal(["exclamation mark", "missing \"%25\"", "missing \"30 Kasım\"", "new numbers: 35"], issues);
    }

    [Theory]
    [InlineData("tr", "1.240,50 TL")]
    [InlineData("en", "TRY 1,240.50")]
    public void Prices_are_formatted_by_code(string language, string expected) =>
        Assert.Equal(expected, LocalizedPrice.Format(1240.50m, language));
}
