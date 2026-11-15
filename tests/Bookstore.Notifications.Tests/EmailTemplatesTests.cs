using System.Globalization;
using Bookstore.Notifications.Samples;
using Bookstore.Notifications.Templates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Notifications.Tests;

public sealed class EmailTemplatesTests
{
    private static EmailTemplates CreateTemplates() =>
        new ServiceCollection()
            .AddNotifications(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Notifications:FileStore:RootPath"] = "files" }).Build())
            .BuildServiceProvider()
            .GetRequiredService<EmailTemplates>();

    [Theory]
    [InlineData("tr-TR", "2 satın alma onayı sizi bekliyor", "10 Kasım 2026")]
    [InlineData("en-US", "2 purchase approvals are waiting for you", "10 November 2026")]
    public async Task Reminder_is_rendered_in_the_approvers_language(string culture, string subject, string date)
    {
        EmailMessage message = await CreateTemplates().ApprovalReminderAsync(
            BookstoreSampleData.ApprovalReminder("Nevin"), new EmailAddress("nevin@example.com"), CultureInfo.GetCultureInfo(culture));

        Assert.Equal(subject, message.Subject);
        Assert.Contains(date, message.HtmlBody);
        Assert.Contains("₺9.120,00", message.HtmlBody);
    }

    [Fact]
    public async Task User_input_is_html_encoded_and_css_is_inlined()
    {
        EmailMessage message = await CreateTemplates().ApprovalReminderAsync(
            BookstoreSampleData.ApprovalReminder("Nevin"), new EmailAddress("nevin@example.com"), CultureInfo.GetCultureInfo("en-US"));

        Assert.DoesNotContain("<script>", message.HtmlBody);
        Assert.Contains("&lt;script&gt;alert('x')&lt;/script&gt;", message.HtmlBody);
        Assert.DoesNotContain("<style>", message.HtmlBody);
        Assert.Contains("style=\"background-color: #0284c7", message.HtmlBody);
    }
}
