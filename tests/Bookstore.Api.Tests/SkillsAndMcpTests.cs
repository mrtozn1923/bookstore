using System.Text;
using System.Text.Json;
using Anthropic.Models.Beta.Messages;
using Bookstore.Api.Catalog;
using Bookstore.Api.Ordering;
using Bookstore.Api.Reports;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Tests;

public sealed class SkillsAndMcpTests
{
    private static readonly SalesReportOptions WithMcp = new()
    {
        InventoryMcpUrl = "https://mcp.example.com/bookstore",
        InventoryMcpToken = "fake-mcp-token-for-tests",
    };

    // Shapes follow the Files API, code execution and Skills pages; ids are made up for the test.
    private const string CsvUploaded = """
        {"id":"file_sales_csv","type":"file","filename":"sales-2026-11.csv","mime_type":"text/csv",
         "size_bytes":214,"created_at":"2026-11-15T09:00:00Z","downloadable":false,"expires_at":"2026-11-15T10:00:00Z"}
        """;

    private const string Paused = """
        {"id":"msg_report_1","type":"message","role":"assistant","model":"claude-sonnet-5-5",
         "container":{"id":"container_report_test","expires_at":"2026-11-15T09:10:00Z"},
         "content":[{"type":"server_tool_use","id":"srvtoolu_01B3C4D5E6F7G8H9I0J1K2L3","name":"bash_code_execution",
                     "input":{"command":"python /tmp/build_report.py"}}],
         "stop_reason":"pause_turn","usage":{"input_tokens":2100,"output_tokens":640}}
        """;

    private const string Finished = """
        {"id":"msg_report_2","type":"message","role":"assistant","model":"claude-sonnet-5-5",
         "container":{"id":"container_report_test","expires_at":"2026-11-15T09:15:00Z"},
         "content":[
           {"type":"bash_code_execution_tool_result","tool_use_id":"srvtoolu_01B3C4D5E6F7G8H9I0J1K2L3",
            "content":{"type":"bash_code_execution_result","stdout":"sales-report-2026-11.xlsx","stderr":"","return_code":0,
                       "content":[{"type":"bash_code_execution_output","file_id":"file_report_xlsx"}]}},
           {"type":"text","text":"November had three book sales."}],
         "stop_reason":"end_turn","usage":{"input_tokens":2900,"output_tokens":210}}
        """;

    private const string ReportMetadata = """
        {"id":"file_report_xlsx","type":"file","filename":"sales-report-2026-11.xlsx",
         "mime_type":"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
         "size_bytes":8,"created_at":"2026-11-15T09:14:00Z","downloadable":true,"expires_at":null}
        """;

    private const string Deleted = """{"id":"file_report_xlsx","type":"file_deleted"}""";

    [Fact]
    public void BuildRequest_WithMcp_SendsSkillCodeExecutionAndAnAllowlistedToolset()
    {
        string body = JsonSerializer.Serialize(SalesReportService.BuildRequest([], null, WithMcp).RawBodyData);
        JsonElement root = JsonDocument.Parse(body).RootElement;

        Assert.Equal("""{"skills":[{"type":"anthropic","skill_id":"xlsx","version":"latest"}]}""",
            root.GetProperty("container").GetRawText());
        JsonElement tools = root.GetProperty("tools");
        Assert.Equal("code_execution_20250825", tools[0].GetProperty("type").GetString());
        Assert.Equal("mcp_toolset", tools[1].GetProperty("type").GetString());
        Assert.False(tools[1].GetProperty("default_config").GetProperty("enabled").GetBoolean());
        Assert.True(tools[1].GetProperty("configs").GetProperty("get_stock_levels").GetProperty("enabled").GetBoolean());
        Assert.Equal(tools[1].GetProperty("mcp_server_name").GetString(), root.GetProperty("mcp_servers")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void BuildRequest_WithoutMcp_HasNoServersAndNoBetaHeader()
    {
        MessageCreateParams request = SalesReportService.BuildRequest([], "container_report_test", new SalesReportOptions());
        JsonElement root = JsonSerializer.SerializeToElement(request.RawBodyData);

        Assert.False(root.TryGetProperty("mcp_servers", out _));
        Assert.Equal(1, root.GetProperty("tools").GetArrayLength());
        Assert.Equal("container_report_test", root.GetProperty("container").GetProperty("id").GetString());
        Assert.Null(request.Betas);
    }

    [Fact]
    public async Task CreateAsync_ContinuesAfterPauseTurnInTheSameContainerAndDownloadsTheWorkbook()
    {
        FakeClaudeHandler handler = new(
            new FakeResponse(CsvUploaded),
            new FakeResponse(Paused),
            new FakeResponse(Finished),
            new FakeResponse(ReportMetadata),
            new FakeResponse("PK-fake-", "application/octet-stream"),
            new FakeResponse(Deleted),
            new FakeResponse("""{"id":"file_sales_csv","type":"file_deleted"}"""));
        SalesReportService service = new(handler.CreateClient(), new InMemoryOrderRepository(), new InMemoryBookRepository(),
            Options.Create(WithMcp), NullLogger<SalesReportService>.Instance);

        SalesReport report = await service.CreateAsync(2026, 11, CancellationToken.None);

        Assert.Equal(2, report.Requests);
        Assert.Equal("sales-report-2026-11.xlsx", report.Files.Single().FileName);
        Assert.Equal("PK-fake-", Encoding.ASCII.GetString(report.Files.Single().Content));
        Assert.Equal("November had three book sales.", report.Summary);

        // The CSV went into the sandbox with a container_upload block, and the second request reused the container.
        JsonElement first = JsonDocument.Parse(handler.RequestBodies[1]).RootElement;
        JsonElement second = JsonDocument.Parse(handler.RequestBodies[2]).RootElement;
        Assert.Equal("file_sales_csv", first.GetProperty("messages")[0].GetProperty("content")[0].GetProperty("file_id").GetString());
        Assert.Equal("container_report_test", second.GetProperty("container").GetProperty("id").GetString());
        Assert.Equal("assistant", second.GetProperty("messages")[1].GetProperty("role").GetString());
        Assert.Contains("mcp-client-2026-09-15", handler.Requests[1].Headers.GetValues("anthropic-beta"));

        Assert.Equal(["POST /v1/files", "POST /v1/messages", "POST /v1/messages", "GET /v1/files/file_report_xlsx",
                      "GET /v1/files/file_report_xlsx/content", "DELETE /v1/files/file_report_xlsx", "DELETE /v1/files/file_sales_csv"],
            handler.Requests.Select(r => $"{r.Method} {r.RequestUri!.AbsolutePath}"));
    }

    [Fact]
    public void SalesCsv_HasNoCustomerData()
    {
        IReadOnlyList<Order> november = new InMemoryOrderRepository()
            .PlacedBetween(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc));

        string csv = SalesCsv.Build(november, new InMemoryBookRepository());

        Assert.Equal(4, csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.DoesNotContain(InMemoryOrderRepository.Ada.ToString(), csv);
        Assert.StartsWith("order_id,placed_at_utc,status,isbn,title,price_try\n1042,2026-11-12T09:30:00Z,Shipped,9780441172719,\"Dune\",420.00", csv);
    }

    [Fact]
    public async Task UploadSkill_SendsPathQualifiedFileNames()
    {
        // Field names follow the SDK's Skill type; the values are made up for the test.
        FakeClaudeHandler handler = new("""
            {"id":"skill_test_bookstore_report","type":"skill","display_name":"bookstore-report","source":"custom",
             "latest_version_id":"skver_test_1","created_at":"2026-11-15T09:00:00Z","updated_at":"2026-11-15T09:00:00Z"}
            """);
        string directory = Path.Combine(AppContext.BaseDirectory, "Reports", "Skills", "bookstore-report");

        Anthropic.Models.Skills.Skill skill = await SkillUploader.UploadAsync(handler.CreateClient(), directory, CancellationToken.None);

        Assert.Equal("skill_test_bookstore_report", skill.ID);
        Assert.Equal("/v1/skills", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Contains("filename=\"bookstore-report/SKILL.md\"", handler.RequestBodies[0]);
        Assert.Contains("name: bookstore-report", handler.RequestBodies[0]);
    }

    [Fact]
    public void BuildRequest_WithConventionsSkill_AddsACustomSkill()
    {
        SalesReportOptions settings = new() { ConventionsSkillId = "skill_test_bookstore_report" };
        JsonElement skills = JsonSerializer.SerializeToElement(SalesReportService.BuildRequest([], null, settings).RawBodyData)
            .GetProperty("container").GetProperty("skills");

        Assert.Equal("""{"type":"custom","skill_id":"skill_test_bookstore_report","version":"latest"}""", skills[1].GetRawText());
    }
}
