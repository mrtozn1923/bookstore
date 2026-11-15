using Bookstore.Notifications.Outbox;
using Bookstore.Notifications.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Bookstore.Notifications.Tests;

public sealed class OutboxEmailDispatcherTests : IAsyncDisposable
{
    private sealed class OutboxTestContext(DbContextOptions<OutboxTestContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyEmailOutbox();
    }

    // Fails with the given exceptions first, then succeeds
    private sealed class ScriptedSender(params EmailDeliveryException[] failures) : IEmailSender
    {
        private readonly Queue<EmailDeliveryException> _failures = new(failures);
        public List<string> SentMessageIds { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (_failures.TryDequeue(out EmailDeliveryException? failure))
            {
                throw failure;
            }

            SentMessageIds.Add(message.MessageId!);
            return Task.CompletedTask;
        }
    }

    private static readonly EmailDeliveryException Transient = new("SMTP 451 RecipientNotAccepted", isTransient: true);
    private static readonly EmailDeliveryException Permanent = new("SMTP 550 RecipientNotAccepted", isTransient: false);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero));
    private readonly ServiceProvider _services;
    private readonly IOptions<EmailOutboxOptions> _options = Options.Create(new EmailOutboxOptions
    {
        RetryDelays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)],
    });

    public OutboxEmailDispatcherTests()
    {
        _connection.Open();
        _services = new ServiceCollection()
            .AddDbContext<OutboxTestContext>(o => o.UseSqlite(_connection))
            .BuildServiceProvider();
        using IServiceScope scope = _services.CreateScope();
        scope.ServiceProvider.GetRequiredService<OutboxTestContext>().Database.EnsureCreated();
    }

    [Fact]
    public async Task Same_key_is_queued_only_once()
    {
        EmailOutbox outbox = new(_time, _options);
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        OutboxTestContext db = scope.ServiceProvider.GetRequiredService<OutboxTestContext>();

        Assert.True(await outbox.EnqueueAsync(db, "order-confirmed:1042", Message(), TestContext.Current.CancellationToken));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.False(await outbox.EnqueueAsync(db, "order-confirmed:1042", Message(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Transient_failures_are_retried_on_schedule_with_the_same_message_id()
    {
        ScriptedSender sender = new(Transient, Transient);
        OutboxEmailDispatcher<OutboxTestContext> dispatcher = await DispatcherWithOneEmailAsync(sender);
        CancellationToken ct = TestContext.Current.CancellationToken;

        await dispatcher.DispatchDueAsync(ct);                       // attempt 1 fails
        _time.Advance(TimeSpan.FromSeconds(59));
        Assert.Equal(0, await dispatcher.DispatchDueAsync(ct));      // not due yet
        _time.Advance(TimeSpan.FromSeconds(1));
        await dispatcher.DispatchDueAsync(ct);                       // attempt 2 fails
        _time.Advance(TimeSpan.FromMinutes(5));
        await dispatcher.DispatchDueAsync(ct);                       // attempt 3 succeeds

        OutboxEmail email = await SingleAsync();
        Assert.Equal(OutboxEmailStatus.Sent, email.Status);
        Assert.Equal(3, email.Attempts);
        Assert.Equal(email.MessageId, Assert.Single(sender.SentMessageIds));
    }

    [Fact]
    public async Task Permanent_failure_is_not_retried()
    {
        OutboxEmailDispatcher<OutboxTestContext> dispatcher = await DispatcherWithOneEmailAsync(new ScriptedSender(Permanent));

        await dispatcher.DispatchDueAsync(TestContext.Current.CancellationToken);

        OutboxEmail email = await SingleAsync();
        Assert.Equal(OutboxEmailStatus.Failed, email.Status);
        Assert.Equal(1, email.Attempts);
        Assert.StartsWith("SMTP 550", email.LastError);
    }

    [Fact]
    public async Task Gives_up_after_the_last_retry_delay()
    {
        OutboxEmailDispatcher<OutboxTestContext> dispatcher = await DispatcherWithOneEmailAsync(new ScriptedSender(Transient, Transient, Transient, Transient));
        CancellationToken ct = TestContext.Current.CancellationToken;

        for (int i = 0; i < 3; i++)
        {
            await dispatcher.DispatchDueAsync(ct);
            _time.Advance(TimeSpan.FromHours(1));
        }

        OutboxEmail email = await SingleAsync();
        Assert.Equal(OutboxEmailStatus.Failed, email.Status);
        Assert.Equal(3, email.Attempts);
    }

    private static EmailMessage Message() => new()
    {
        To = new EmailAddress("nevin@example.com", "Nevin"),
        Subject = "2 purchase approvals are waiting for you",
        HtmlBody = "<p>Hello</p>",
        TextBody = "Hello",
    };

    private async Task<OutboxEmailDispatcher<OutboxTestContext>> DispatcherWithOneEmailAsync(IEmailSender sender, StoredAttachment? attachment = null)
    {
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            OutboxTestContext db = scope.ServiceProvider.GetRequiredService<OutboxTestContext>();
            await new EmailOutbox(_time, _options).EnqueueAsync(db, "approval-reminder:nevin@example.com:2026-11-15", Message(), attachment);
            await db.SaveChangesAsync();
        }

        LocalFileStore files = new(Options.Create(new LocalFileStoreOptions { RootPath = Path.Combine(Path.GetTempPath(), "bookstore-outbox-" + Guid.NewGuid().ToString("N")) }));
        return new OutboxEmailDispatcher<OutboxTestContext>(_services.GetRequiredService<IServiceScopeFactory>(), sender, files, _time, _options,
            NullLogger<OutboxEmailDispatcher<OutboxTestContext>>.Instance);
    }

    [Fact]
    public async Task Missing_attachment_fails_the_email_for_good()
    {
        FakeEmailSender sender = new();
        OutboxEmailDispatcher<OutboxTestContext> dispatcher = await DispatcherWithOneEmailAsync(sender,
            new StoredAttachment("invoices/2026/BKS-2026-009999.pdf", "invoice.pdf", "application/pdf"));

        await dispatcher.DispatchDueAsync(TestContext.Current.CancellationToken);

        OutboxEmail email = await SingleAsync();
        Assert.Equal((OutboxEmailStatus.Failed, 1), (email.Status, email.Attempts));
        Assert.Empty(sender.Sent);
    }

    private async Task<OutboxEmail> SingleAsync()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OutboxTestContext>().Set<OutboxEmail>().SingleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
