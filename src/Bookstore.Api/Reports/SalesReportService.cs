using System.Text;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using Anthropic.Models.Files;
using Bookstore.Api.Catalog;
using Bookstore.Api.Ordering;
using Microsoft.Extensions.Options;

namespace Bookstore.Api.Reports;

public sealed record ReportFile(string FileName, byte[] Content);

public sealed record SalesReport(string Summary, IReadOnlyList<ReportFile> Files, int Requests);

public sealed class SalesReportService(
    IAnthropicClient claude,
    IOrderRepository orders,
    IBookRepository books,
    IOptions<SalesReportOptions> options,
    ILogger<SalesReportService> logger)
{
    public const string InventoryServerName = "bookstore-inventory";

    // A long skill run can stop with pause_turn; we continue it a few times, then give up.
    public const int MaxRequests = 4;

    public static MessageCreateParams BuildRequest(
        IReadOnlyList<BetaMessageParam> messages, string? containerId, SalesReportOptions settings)
    {
        bool useInventory = !string.IsNullOrWhiteSpace(settings.InventoryMcpUrl);
        List<BetaToolUnion> tools = [new BetaCodeExecutionTool20250825()];
        if (useInventory)
        {
            // mcp_servers and mcp_toolset go together: every server must be used by exactly one toolset.
            tools.Add(new BetaMcpToolset
            {
                McpServerName = InventoryServerName,
                // Allowlist: only the read-only stock tool, whatever else the server offers.
                DefaultConfig = new BetaMcpToolDefaultConfig { Enabled = false },
                Configs = new Dictionary<string, BetaMcpToolConfig> { ["get_stock_levels"] = new() { Enabled = true } },
            });
        }

        List<BetaSkillParams> skills = [new() { Type = BetaSkillParamsType.Anthropic, SkillID = "xlsx", Version = "latest" }];
        if (!string.IsNullOrWhiteSpace(settings.ConventionsSkillId))
        {
            skills.Add(new() { Type = BetaSkillParamsType.Custom, SkillID = settings.ConventionsSkillId, Version = "latest" });
        }

        BetaContainerParams container = new() { Skills = skills };

        // The SDK serializes a property when it is set: build lists first, assign them once.
        MessageCreateParams request = new()
        {
            Model = settings.Model,
            MaxTokens = 8192,
            // After the first request the same container keeps the files of this turn; ID = null would send "id": null.
            Container = containerId is null ? container : container with { ID = containerId },
            Messages = messages,
            Tools = tools,
        };

        if (!useInventory)
        {
            return request;
        }

        return request with
        {
            McpServers =
            [
                new BetaRequestMcpServerUrlDefinition
                {
                    Name = InventoryServerName,
                    Url = settings.InventoryMcpUrl!,
                    AuthorizationToken = settings.InventoryMcpToken,
                },
            ],
            Betas = [AnthropicBeta.McpClient2026_09_15],
        };
    }

    public async Task<SalesReport> CreateAsync(int year, int month, CancellationToken cancellationToken)
    {
        DateTime from = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        string csv = SalesCsv.Build(orders.PlacedBetween(from, from.AddMonths(1)), books);

        FileMetadata upload = await claude.Files.Upload(new FileUploadParams
        {
            File = new BinaryContent
            {
                Stream = new MemoryStream(Encoding.UTF8.GetBytes(csv)),
                FileName = $"sales-{year}-{month:00}.csv",
                ContentType = new("text/csv"),
            },
            ExpiresInSeconds = 3_600,
        }, cancellationToken);

        try
        {
            List<BetaMessageParam> messages =
            [
                new BetaUserMessageParam
                {
                    Content = new List<BetaContentBlockParam>
                    {
                        new BetaContainerUploadBlockParam { FileID = upload.ID },
                        new BetaTextBlockParam
                        {
                            Text = $"""
                                The uploaded CSV has Bookstore's book sales for {year}-{month:00}.
                                Build an Excel management report with the xlsx skill: revenue per day, top titles and a chart.
                                If the inventory tool is available, add a sheet with the current stock of the top titles.
                                Save the workbook as sales-report-{year}-{month:00}.xlsx in $OUTPUT_DIR and list the directory.
                                Then reply with a three-sentence summary.
                                """,
                        },
                    },
                },
            ];

            string? containerId = null;
            List<string> outputFileIds = [];
            BetaMessage response;
            int requests = 0;
            do
            {
                response = await claude.Beta.Messages.Create(BuildRequest(messages, containerId, options.Value), cancellationToken);
                requests++;
                containerId = response.Container?.ID ?? containerId;
                outputFileIds.AddRange(OutputFileIds(response));

                // pause_turn: send the turn back unchanged in the same container so Claude can continue.
                messages.Add(new BetaAssistantMessageParam
                {
                    Content = response.Content.Select(block => new BetaContentBlockParam(block.Json)).ToList(),
                });
            }
            while (response.StopReason?.Value() == BetaStopReason.PauseTurn && requests < MaxRequests);

            List<ReportFile> files = [];
            foreach (string fileId in outputFileIds.Distinct())
            {
                files.Add(await DownloadAndDeleteAsync(fileId, cancellationToken));
            }

            string summary = string.Concat(response.Content.Select(b => b.TryPickText(out BetaTextBlock? t) ? t.Text : string.Empty));
            logger.LogInformation("Sales report {Year}-{Month}: stop={StopReason} requests={Requests} files={Files}",
                year, month, response.StopReason?.Raw(), requests, files.Count);
            return new SalesReport(summary.Trim(), files, requests);
        }
        finally
        {
            // The CSV was only for this run.
            await claude.Files.Delete(upload.ID, cancellationToken: CancellationToken.None);
        }
    }

    // Files written to $OUTPUT_DIR come back as file ids inside bash_code_execution results.
    public static IEnumerable<string> OutputFileIds(BetaMessage response) =>
        response.Content
            .Select(block => block.TryPickBashCodeExecutionToolResult(out BetaBashCodeExecutionToolResultBlock? result) ? result : null)
            .Where(result => result is not null)
            .SelectMany(result => result!.Content.TryPickBetaBashCodeExecutionResultBlock(out BetaBashCodeExecutionResultBlock? ok)
                ? ok.Content.Select(output => output.FileID)
                : []);

    private async Task<ReportFile> DownloadAndDeleteAsync(string fileId, CancellationToken cancellationToken)
    {
        FileMetadata metadata = await claude.Files.RetrieveMetadata(fileId, cancellationToken: cancellationToken);
        using Anthropic.Core.HttpResponse download = await claude.Files.Download(fileId, cancellationToken: cancellationToken);
        await using Stream content = await download.ReadAsStream(cancellationToken);
        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer, cancellationToken);

        // Generated files count toward the organization's storage until deleted.
        await claude.Files.Delete(fileId, cancellationToken: cancellationToken);
        return new ReportFile(metadata.Filename, buffer.ToArray());
    }
}
