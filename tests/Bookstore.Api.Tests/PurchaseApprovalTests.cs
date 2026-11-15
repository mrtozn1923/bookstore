using System.Net;
using System.Net.Http.Json;
using Bookstore.Institutional;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Tests;

// Threats T09–T11 of threat-model/bookstore-checkout.json ("Institutional purchase approval" diagram).
public sealed class PurchaseApprovalTests(BookstoreApiFactory factory)
{
    private const string Middlemarch = "9780141439549";    // 389.00 → 350.10 with the institutional discount
    private const string Frankenstein = "9780141439471";   // 219.90 → 197.91
    private const string MobyDick = "9780142437247";       // 359.00 → 323.10

    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    [Trait("threat", "T11")]
    public async Task Request_above_the_unit_limit_waits_for_approval_and_cannot_be_ordered()
    {
        HttpClient ada = factory.CreateClientAs(TestUsers.Ada);
        PurchaseRequestView request = await CreateAsync(ada, (Middlemarch, 20));   // 7,002.00 > 5,000.00

        Assert.Equal("PendingApproval", request.Status);
        HttpResponseMessage order = await ada.PostAsync($"/api/institutions/me/purchase-requests/{request.Id}/order", null, ct);
        Assert.Equal(HttpStatusCode.Conflict, order.StatusCode);
    }

    [Fact]
    [Trait("threat", "T09")]
    public async Task Buyer_with_both_roles_cannot_approve_own_request()
    {
        HttpClient linus = factory.CreateClientAs(TestUsers.Linus);
        PurchaseRequestView request = await CreateAsync(linus, (Middlemarch, 20));

        HttpResponseMessage approve = await linus.PostAsync($"/api/institutions/me/purchase-requests/{request.Id}/approve", null, ct);

        Assert.Equal(HttpStatusCode.Forbidden, approve.StatusCode);
        ProblemDetails? problem = await approve.Content.ReadFromJsonAsync<ProblemDetails>(ct);
        Assert.Equal("The requester cannot approve their own request.", problem?.Detail);
    }

    [Fact]
    [Trait("threat", "T09")]
    public async Task Approver_of_another_institution_does_not_see_the_request()
    {
        PurchaseRequestView request = await CreateAsync(factory.CreateClientAs(TestUsers.Ada), (Middlemarch, 20));

        HttpResponseMessage approve = await factory.CreateClientAs(TestUsers.Alan)
            .PostAsync($"/api/institutions/me/purchase-requests/{request.Id}/approve", null, ct);

        Assert.Equal(HttpStatusCode.NotFound, approve.StatusCode);
    }

    [Fact]
    [Trait("threat", "T10")]
    public async Task Changing_lines_after_approval_needs_a_new_approval()
    {
        HttpClient ada = factory.CreateClientAs(TestUsers.Ada);
        PurchaseRequestView request = await CreateAsync(ada, (Middlemarch, 20));
        await ApproveAsync(factory.CreateClientAs(TestUsers.Grace), request.Id);

        HttpResponseMessage change = await ada.PutAsJsonAsync($"/api/institutions/me/purchase-requests/{request.Id}/lines",
            new PurchaseRequestBody([new RequestLine(Middlemarch, 25)]), ct);
        PurchaseRequestView? changed = await change.Content.ReadFromJsonAsync<PurchaseRequestView>(ct);

        Assert.Equal("PendingApproval", changed?.Status);
        Assert.Null(changed?.ApprovedBy);
        HttpResponseMessage order = await ada.PostAsync($"/api/institutions/me/purchase-requests/{request.Id}/order", null, ct);
        Assert.Equal(HttpStatusCode.Conflict, order.StatusCode);
    }

    [Fact]
    [Trait("threat", "T09")]
    [Trait("threat", "T11")]
    public async Task Approved_request_is_ordered_with_the_institutional_price()
    {
        HttpClient ada = factory.CreateClientAs(TestUsers.Ada);
        PurchaseRequestView request = await CreateAsync(ada, (MobyDick, 12), (Frankenstein, 10));   // 3,877.20 + 1,979.10
        Assert.Equal(5856.30m, request.Total);
        await ApproveAsync(factory.CreateClientAs(TestUsers.Grace), request.Id);

        HttpResponseMessage order = await ada.PostAsync($"/api/institutions/me/purchase-requests/{request.Id}/order", null, ct);

        Assert.Equal(HttpStatusCode.OK, order.StatusCode);
        PurchaseRequestView? ordered = await order.Content.ReadFromJsonAsync<PurchaseRequestView>(ct);
        Assert.Equal("Ordered", ordered?.Status);
        Assert.Equal("grace", ordered?.ApprovedBy);
        Assert.NotNull(ordered?.OrderId);
    }

    [Fact]
    public async Task Fewer_than_twenty_copies_is_not_an_institutional_order()
    {
        HttpResponseMessage response = await factory.CreateClientAs(TestUsers.Ada).PostAsJsonAsync(
            "/api/institutions/me/purchase-requests", new PurchaseRequestBody([new RequestLine(Frankenstein, 19)]), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<PurchaseRequestView> CreateAsync(HttpClient client, params (string Isbn, int Quantity)[] lines)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/institutions/me/purchase-requests",
            new PurchaseRequestBody([.. lines.Select(l => new RequestLine(l.Isbn, l.Quantity))]), ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PurchaseRequestView>(ct))!;
    }

    private async Task ApproveAsync(HttpClient approver, int requestId)
    {
        HttpResponseMessage response = await approver.PostAsync($"/api/institutions/me/purchase-requests/{requestId}/approve", null, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
