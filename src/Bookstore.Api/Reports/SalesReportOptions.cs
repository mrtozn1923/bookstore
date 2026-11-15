namespace Bookstore.Api.Reports;

public sealed class SalesReportOptions
{
    public const string SectionName = "SalesReport";

    public string Model { get; set; } = "claude-sonnet-5-5";

    // skill_... id of the uploaded bookstore-report skill (SkillUploader). Empty: only the Anthropic xlsx skill.
    public string? ConventionsSkillId { get; set; }

    // Remote MCP server with Bookstore's inventory tools (AI Agents and MCP series). Empty: no MCP connector.
    public string? InventoryMcpUrl { get; set; }

    // A read-only token for that server, from user secrets or a vault; never from appsettings.json.
    public string? InventoryMcpToken { get; set; }
}
