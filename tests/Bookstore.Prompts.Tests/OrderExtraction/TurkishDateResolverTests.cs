using Bookstore.Prompts.Tasks.OrderExtraction;

namespace Bookstore.Prompts.Tests.OrderExtraction;

public class TurkishDateResolverTests
{
    [Theory]
    [InlineData("2026-11-12", "gelecek salıya kadar", "2026-11-17")] // Thursday -> next week's Tuesday
    [InlineData("2026-11-25", "Önümüzdeki çarşamba", "2026-12-02")] // same weekday -> a week later
    [InlineData("2026-11-24", "Yarın gelirse", "2026-11-25")]
    [InlineData("2026-11-18", "Ayın 30'una kadar", "2026-11-30")]
    [InlineData("2026-12-03", "15 Aralık'tan önce", "2026-12-15")]
    public void Resolves_known_phrases(string sent, string phrase, string expected)
    {
        DateResolution result = TurkishDateResolver.Resolve(phrase, DateOnly.Parse(sent));

        Assert.Equal(DateOnly.Parse(expected), result.Date);
        Assert.False(result.Ambiguous);
    }

    [Fact]
    public void Next_friday_said_on_a_monday_is_ambiguous()
    {
        DateResolution result = TurkishDateResolver.Resolve("gelecek cuma", new DateOnly(2026, 11, 16));

        Assert.Null(result.Date);
        Assert.True(result.Ambiguous);
    }

    [Fact]
    public void Unknown_phrase_gives_no_date()
    {
        DateResolution result = TurkishDateResolver.Resolve("Teslim tarihi konusunda acelemiz yok", new DateOnly(2026, 11, 20));

        Assert.Null(result.Date);
        Assert.False(result.Ambiguous);
    }
}
