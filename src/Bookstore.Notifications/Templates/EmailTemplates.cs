using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PreMailer.Net;

namespace Bookstore.Notifications.Templates;

public sealed class EmailTemplateOptions
{
    public bool InlineCss { get; set; } = true;
}

public sealed class EmailTemplates(
    IServiceProvider services,
    ILoggerFactory loggerFactory,
    IStringLocalizer<EmailText> text,
    IOptions<EmailTemplateOptions> options,
    ILogger<EmailTemplates> logger)
{
    public async Task<EmailMessage> ApprovalReminderAsync(ApprovalReminderModel model, EmailAddress to, CultureInfo culture)
    {
        // Culture set here flows into the renderer's dispatcher; it is restored when this method returns
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        string html = await RenderAsync<PurchaseApprovalReminder>(new Dictionary<string, object?> { ["Model"] = model });
        return new EmailMessage
        {
            To = to,
            // "1 purchase approvals" was the first English output: plural forms need their own strings
            Subject = model.Pending.Count == 1 ? text["ReminderSubjectOne"] : text["ReminderSubject", model.Pending.Count],
            HtmlBody = html,
            TextBody = HtmlToText(html),
        };
    }

    private async Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters) where TComponent : IComponent
    {
        await using HtmlRenderer renderer = new(services, loggerFactory);
        string html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            HtmlRootComponent output = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
            return output.ToHtmlString();
        });

        if (!options.Value.InlineCss)
        {
            return html;
        }

        // Many email clients ignore <style>; copy every rule into style="" attributes
        InlineResult inlined = PreMailer.Net.PreMailer.MoveCssInline(html, removeStyleElements: true);
        foreach (string warning in inlined.Warnings)
        {
            logger.LogWarning("CSS inliner: {Warning}", warning);
        }

        return inlined.Html;
    }

    // Plain-text alternative built from the same HTML, so the two never drift apart
    public static string HtmlToText(string html)
    {
        IDocument document = new HtmlParser().ParseDocument(html);
        StringBuilder text = new();
        Walk(document.Body!, text);
        return string.Join("\n", text.ToString().Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    }

    private static void Walk(INode node, StringBuilder text)
    {
        foreach (INode child in node.ChildNodes)
        {
            if (child is IText t)
            {
                text.Append(Regex.Replace(t.Data, @"\s+", " "));
                continue;
            }

            if (child is not IElement element)
            {
                continue;
            }

            Walk(element, text);
            switch (element.LocalName)
            {
                case "td" or "th":
                    text.Append("  ");
                    break;
                case "tr" or "p" or "div" or "h1" or "h2" or "h3":
                    text.Append('\n');
                    break;
                case "a" when element.GetAttribute("href") is { } href:
                    text.Append($" ({href})");
                    break;
            }
        }
    }
}
