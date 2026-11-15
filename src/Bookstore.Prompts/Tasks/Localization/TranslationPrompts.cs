using System.Text;
using Bookstore.Prompts.Evaluation;

namespace Bookstore.Prompts.Tasks.Localization;

public static class TranslationPrompts
{
    public static readonly PromptVersion V1 = new(
        Task: "description-translation",
        Version: "v1",
        System: "Translate the book description from Turkish to English.",
        BuildUser: description => description);

    public static PromptVersion V2(Glossary glossary)
    {
        StringBuilder system = new("""
            You translate Bookstore catalog descriptions from Turkish to English for the English
            version of the store. The reader is a book buyer; keep the tone warm and factual.

            ## Rules
            - Output only the translated HTML. No introduction, no notes.
            - Keep every HTML tag and every placeholder in curly braces, such as {price}, exactly
              as they are. Do not translate or fill placeholders.
            - Do not add facts, dates or numbers that are not in the source.
            - Use the glossary below for these terms.

            ## Glossary (Turkish -> English)

            """);
        foreach ((string tr, string en) in glossary.Terms)
        {
            system.AppendLine($"- {tr} -> {en}");
        }
        system.AppendLine();
        system.AppendLine($"## Never translate: {string.Join(", ", glossary.DoNotTranslate)}");

        return new PromptVersion(
            Task: "description-translation",
            Version: "v2",
            System: system.ToString().TrimEnd(),
            BuildUser: description => $"<source lang=\"tr\">\n{description}\n</source>");
    }

    // v3: fewer rules, glossary inline, one worked example of the exact output shape
    public static PromptVersion V3(Glossary glossary) => new(
        Task: "description-translation",
        Version: "v3",
        System: $$"""
            Translate Bookstore catalog descriptions from Turkish to English.
            Return the same HTML with only the Turkish text translated: same tags, no new
            attributes, no code fences, no notes. Placeholders such as {price} stay as they are.
            Book titles and the store name Bookstore stay in English as written.
            Glossary: {{string.Join("; ", glossary.Terms.Select(t => $"{t.Key} = {t.Value}"))}}.

            Example input:
            <p><b>Walden</b>, Thoreau'nun 1854 tarihli kitabı. Fiyat: {price}</p>
            Example output:
            <p><b>Walden</b>, Thoreau's book from 1854. Price: {price}</p>
            """,
        BuildUser: description => description);
}
