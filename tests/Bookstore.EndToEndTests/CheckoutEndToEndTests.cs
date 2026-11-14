using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bookstore.EndToEndTests;

public sealed record OrderStatusChanged(Guid OrderId, string Status, string? Reason);

// The whole system from the AppHost: SQL Server, RabbitMQ, Redis, Keycloak, Mailpit and every service.
// The test does what Angular does: signs in through the BFF, listens on the hub, places an order.
public sealed partial class CheckoutEndToEndTests
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(4);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_placed_order_is_confirmed_and_the_browser_is_told()
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        // The AppHost's first launch profile is https, which needs a trusted ASP.NET Core developer certificate in
        // every service. A CI agent usually has none; the http profile is what we run locally anyway.
        Environment.SetEnvironmentVariable("DOTNET_LAUNCH_PROFILE", "http");
        // The testing builder gives every endpoint a random port by default. The Keycloak realm (post 10) allows
        // redirects only to http://localhost:5100/signin-oidc, so the BFF and Keycloak keep their fixed ports.
        IDistributedApplicationTestingBuilder builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Bookstore_AppHost>(
            ["DcpPublisher:RandomizePorts=false"], Ct);
        // The Angular watcher is a development convenience; the test talks to the BFF's API directly
        foreach (IResource web in builder.Resources.Where(r => r.Name.StartsWith("web", StringComparison.Ordinal)).ToList())
        {
            builder.Resources.Remove(web);
        }
        // Persistent containers and named volumes are the developer's local environment. The test gets its own
        // throwaway containers (same images, new names, no volumes): every run starts from empty databases.
        foreach (ContainerResource container in builder.Resources.OfType<ContainerResource>().ToList())
        {
            // WithSessionLifetime is marked "for evaluation" (ASPIREPERSISTENCE001) in Aspire 13.6
#pragma warning disable ASPIREPERSISTENCE001
            builder.CreateResourceBuilder(container).WithSessionLifetime();
#pragma warning restore ASPIREPERSISTENCE001
            foreach (IResourceAnnotation volume in container.Annotations.OfType<ContainerMountAnnotation>()
                .Where(m => m.Type == ContainerMountType.Volume).ToList())
            {
                container.Annotations.Remove(volume);
            }
            if (container.Annotations.OfType<ContainerNameAnnotation>().SingleOrDefault() is { } name)
            {
                container.Annotations.Remove(name);
                container.Annotations.Add(new ContainerNameAnnotation { Name = name.Name.Replace("msapp-", "msapp-e2e-") });
            }
        }

        await using DistributedApplication app = await builder.BuildAsync(Ct);
        using CancellationTokenSource startup = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        startup.CancelAfter(StartupTimeout);
        await app.StartAsync(startup.Token);
        // Readiness is a signal from the AppHost (health checks), not a sleep
        foreach (string service in (string[])["bff", "ordering", "inventory", "payments", "notifications"])
        {
            await app.ResourceNotifications.WaitForResourceHealthyAsync(service, startup.Token);
        }
        TestContext.Current.TestOutputHelper?.WriteLine($"system healthy after {elapsed.Elapsed.TotalSeconds:F1} s");

        Uri bff = app.GetEndpoint("bff", "http");
        CookieContainer cookies = new();
        using HttpClient browser = new(new LocalhostCookies(cookies)) { BaseAddress = bff };
        string password = (await builder.Resources.OfType<ParameterResource>().Single(p => p.Name == "keycloak-demo-password").GetValueAsync(Ct))!;
        await SignInAsync(browser, "ada", password);

        // The same hub connection Angular opens, with the same session cookie
        TaskCompletionSource<OrderStatusChanged> confirmed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid orderId = Guid.Empty;
        await using HubConnection hub = new HubConnectionBuilder()
            .WithUrl(new Uri(bff, "/hubs/orders"), options => options.Cookies = cookies)
            .Build();
        hub.On<OrderStatusChanged>("OrderStatusChanged", change =>
        {
            if (change.OrderId == orderId && change.Status is "Confirmed" or "Cancelled")
            {
                confirmed.TrySetResult(change);
            }
        });
        await hub.StartAsync(Ct);

        Stopwatch checkout = Stopwatch.StartNew();
        using HttpRequestMessage place = new(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new { items = new[] { new { isbn = "9780141439587", quantity = 1, unitPrice = 249.90m } } })
        };
        place.Headers.Add("X-CSRF", "1");
        using HttpResponseMessage response = await browser.SendAsync(place, Ct);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        orderId = Guid.Parse(response.Headers.Location!.OriginalString.Split('/')[^1]);

        // The end of the saga is a push, so the test waits for the push: no polling loop, no fixed delay
        OrderStatusChanged result = await confirmed.Task.WaitAsync(TimeSpan.FromSeconds(60), Ct);
        Assert.Equal("Confirmed", result.Status);
        TestContext.Current.TestOutputHelper?.WriteLine($"order {orderId} {result.Status} {checkout.Elapsed.TotalSeconds:F2} s after POST");

        OrderView? order = await browser.GetFromJsonAsync<OrderView>($"/api/orders/{orderId}", Ct);
        Assert.Equal("Confirmed", order?.Status);
    }

    private sealed record OrderView(Guid Id, string Status);

    // Like a browser: follow the redirect to Keycloak, post the login form, post the form_post page back to the BFF
    private static async Task SignInAsync(HttpClient browser, string user, string password)
    {
        string loginPage = await browser.GetStringAsync("/bff/login?returnUrl=/", Ct);
        string action = WebUtility.HtmlDecode(LoginFormAction().Match(loginPage).Groups[1].Value);
        using HttpResponseMessage keycloak = await browser.PostAsync(action,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["username"] = user, ["password"] = password }), Ct);
        string formPost = await keycloak.Content.ReadAsStringAsync(Ct);
        string callback = WebUtility.HtmlDecode(FormAction().Match(formPost).Groups[1].Value);
        Dictionary<string, string> fields = HiddenInput().Matches(formPost)
            .ToDictionary(m => m.Groups[1].Value, m => WebUtility.HtmlDecode(m.Groups[2].Value));
        using HttpResponseMessage signedIn = await browser.PostAsync(callback, new FormUrlEncodedContent(fields), Ct);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/bff/user", Ct)).StatusCode);
    }

    // Browsers treat http://localhost as a secure context and send Secure cookies to it; HttpClient does not.
    // ASP.NET Core's OIDC correlation cookies and Keycloak's session cookies are Secure, so the test clears the flag
    // on every hop of the redirect chain (redirects are followed here, not by HttpClientHandler).
    private sealed class LocalhostCookies(CookieContainer cookies) : DelegatingHandler(
        new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = false })
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            HttpResponseMessage response = await base.SendAsync(request, ct);
            for (int hop = 0; hop < 10 && response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.SeeOther; hop++)
            {
                ClearSecureFlags();
                Uri next = new(request.RequestUri!, response.Headers.Location!);
                response.Dispose();
                request = new HttpRequestMessage(HttpMethod.Get, next);
                response = await base.SendAsync(request, ct);
            }
            ClearSecureFlags();
            return response;
        }

        private void ClearSecureFlags()
        {
            foreach (Cookie cookie in cookies.GetAllCookies())
            {
                cookie.Secure = false;
            }
        }
    }

    [GeneratedRegex("id=\"kc-form-login\"[^>]*action=\"([^\"]+)\"")]
    private static partial Regex LoginFormAction();

    // Keycloak's form_post page is written in upper case: <FORM METHOD="POST" ACTION="...">
    [GeneratedRegex("<form[^>]*action=\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex FormAction();

    [GeneratedRegex("<input type=\"hidden\" name=\"([^\"]+)\" value=\"([^\"]*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex HiddenInput();
}
