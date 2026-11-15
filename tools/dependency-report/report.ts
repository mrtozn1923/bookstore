// Nightly dependency and vulnerability report for the Bookstore repository.
// Run from tools/dependency-report with ANTHROPIC_API_KEY set: npm run report
import { query, type Options, type SDKResultMessage } from "@anthropic-ai/claude-agent-sdk";
import { mkdir, writeFile } from "node:fs/promises";
import path from "node:path";

type Finding = {
  ecosystem: "nuget" | "npm";
  project: string;
  package: string;
  installedVersion: string;
  severity: "low" | "moderate" | "high" | "critical";
  usedIn: string;
  advisory: string;
  fixedIn?: string;
};

type Report = { summary: string; findings: Finding[] };

const repoRoot = path.resolve(import.meta.dirname, "../..");
const reportPath = path.join(repoRoot, "artifacts", "dependency-report.md");

const schema = {
  type: "object",
  properties: {
    summary: { type: "string" },
    findings: {
      type: "array",
      items: {
        type: "object",
        properties: {
          ecosystem: { type: "string", enum: ["nuget", "npm"] },
          project: { type: "string" },
          package: { type: "string" },
          installedVersion: { type: "string" },
          severity: { type: "string", enum: ["low", "moderate", "high", "critical"] },
          usedIn: { type: "string" },
          advisory: { type: "string" },
          fixedIn: { type: "string" },
        },
        required: ["ecosystem", "project", "package", "installedVersion", "severity", "usedIn", "advisory"],
      },
    },
  },
  required: ["summary", "findings"],
};

const prompt = [
  "Produce a dependency vulnerability report for the Bookstore repository.",
  "1. Run `dotnet list Bookstore.slnx package --vulnerable --include-transitive` from the repository root.",
  "2. Run `npm audit --json --prefix web/bookstore-web`.",
  "3. For each vulnerable package, find where Bookstore uses it: the .csproj that references it,",
  "   or the Angular files that import it. Put the file paths in usedIn.",
  "4. Write the advisory in one plain sentence and fill fixedIn when a fixed version is known.",
  "Do not change any file. If a command fails, say so in summary and report what you have.",
].join("\n");

const options: Options = {
  cwd: repoRoot,
  settingSources: [],
  tools: ["Read", "Grep", "Glob", "Bash", "PowerShell"],
  allowedTools: [
    "Bash(dotnet list Bookstore.slnx package --vulnerable *)",
    "Bash(npm audit *)",
    "PowerShell(dotnet list Bookstore.slnx package --vulnerable *)",
    "PowerShell(npm audit *)",
  ],
  disallowedTools: ["Read(**/.env)", "Read(**/.env.*)", "Read(**/appsettings.Production.json)"],
  permissionMode: "dontAsk",
  model: "sonnet",
  maxTurns: 20,
  maxBudgetUsd: 1,
  persistSession: false,
  outputFormat: { type: "json_schema", schema },
};

function toMarkdown(report: Report, result: SDKResultMessage): string {
  const lines = [
    "# Bookstore dependency report",
    "",
    `Generated: ${new Date().toISOString()}`,
    "",
    report.summary,
    "",
    "| Severity | Ecosystem | Package | Version | Fixed in | Used in | Advisory |",
    "| --- | --- | --- | --- | --- | --- | --- |",
    ...report.findings.map((f) =>
      `| ${f.severity} | ${f.ecosystem} | ${f.package} | ${f.installedVersion} | ${f.fixedIn ?? "-"} | ${f.usedIn} | ${f.advisory} |`),
  ];
  if (result.permission_denials.length > 0) {
    lines.push("", "Denied tool calls:", ...result.permission_denials.map((d) => `- ${d.tool_name}: ${JSON.stringify(d.tool_input)}`));
  }
  lines.push("", `Turns: ${result.num_turns}, estimated cost: ${result.total_cost_usd.toFixed(2)} USD`);
  return lines.join("\n") + "\n";
}

let result: SDKResultMessage | undefined;
try {
  for await (const message of query({ prompt, options })) {
    if (message.type === "result") {
      result = message;
    }
  }
} catch (error) {
  // A turn or budget cap ends the run with a result message and then throws.
  console.error(`query ended with an error: ${String(error)}`);
}

if (!result) {
  console.error("No result message.");
  process.exit(1);
}
if (result.subtype !== "success" || result.is_error || !result.structured_output) {
  console.error(`Run failed: ${result.subtype}, estimated cost ${result.total_cost_usd.toFixed(2)} USD`);
  process.exit(1);
}

await mkdir(path.dirname(reportPath), { recursive: true });
await writeFile(reportPath, toMarkdown(result.structured_output as Report, result));
console.log(`Wrote ${path.relative(repoRoot, reportPath)} (${(result.structured_output as Report).findings.length} findings)`);
