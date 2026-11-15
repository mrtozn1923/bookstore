using System.Text.Json;

namespace Bookstore.Web.Tests;

// A few of Google's required properties per feature, read from the Search Central docs on 2026-11-15
public static class GalleryRules
{
    public static IReadOnlyList<string> Check(JsonElement node) => node.GetProperty("@type").GetString() switch
    {
        "Event" => Event(node),
        "DiscussionForumPosting" => Forum(node),
        "QAPage" => QaPage(node),
        "ImageObject" => Image(node),
        var other => [$"no rule for {other}"],
    };

    private static List<string> Event(JsonElement e)
    {
        List<string> problems = Missing(e, "name", "startDate");
        // events must take place at a physical location: a Place with an address
        if (!e.TryGetProperty("location", out JsonElement place) || Type(place) != "Place")
        {
            problems.Add("location must be a Place (virtual-only events are not supported)");
        }
        else
        {
            problems.AddRange(Missing(place, "address").Select(p => $"location.{p}"));
        }
        return problems;
    }

    private static List<string> Forum(JsonElement post)
    {
        List<string> problems = Missing(post, "datePublished");
        if (!post.TryGetProperty("author", out JsonElement author) || !author.TryGetProperty("name", out _))
        {
            problems.Add("author.name is missing");
        }
        if (!HasAny(post, "text", "image", "video"))
        {
            problems.Add("one of text, image or video is required");
        }
        return problems;
    }

    private static List<string> QaPage(JsonElement page)
    {
        // exactly one Question per page, under mainEntity
        if (!page.TryGetProperty("mainEntity", out JsonElement q) || q.ValueKind != JsonValueKind.Object
            || Type(q) != "Question")
        {
            return ["mainEntity must be a single Question"];
        }
        List<string> problems = Missing(q, "name", "answerCount");
        if (!HasAny(q, "acceptedAnswer", "suggestedAnswer"))
        {
            problems.Add("a Question needs acceptedAnswer or suggestedAnswer");
        }
        foreach (JsonElement answer in Answers(q))
        {
            problems.AddRange(Missing(answer, "text").Select(p => $"Answer.{p}"));
        }
        return problems;
    }

    private static List<string> Image(JsonElement image)
    {
        List<string> problems = HasAny(image, "contentUrl", "url") ? [] : ["contentUrl is missing"];
        if (!HasAny(image, "creator", "creditText", "copyrightNotice", "license"))
        {
            problems.Add("one of creator, creditText, copyrightNotice or license is required");
        }
        return problems;
    }

    private static IEnumerable<JsonElement> Answers(JsonElement question)
    {
        foreach (string name in new[] { "acceptedAnswer", "suggestedAnswer" })
        {
            if (question.TryGetProperty(name, out JsonElement value))
            {
                IEnumerable<JsonElement> answers = value.ValueKind == JsonValueKind.Array ? value.EnumerateArray() : [value];
                foreach (JsonElement answer in answers)
                {
                    yield return answer;
                }
            }
        }
    }

    private static List<string> Missing(JsonElement node, params string[] names) =>
        [.. names.Where(n => !node.TryGetProperty(n, out _)).Select(n => $"{n} is missing")];

    private static bool HasAny(JsonElement node, params string[] names) => names.Any(n => node.TryGetProperty(n, out _));

    private static string? Type(JsonElement node) =>
        node.TryGetProperty("@type", out JsonElement t) ? t.GetString() : null;
}
