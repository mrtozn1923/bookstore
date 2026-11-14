// Builds a short pull request comment from `terraform show -no-color tfplan` output
const fs = require("fs");

module.exports = function summary(path, environment) {
  const lines = fs.readFileSync(path, "utf8").split("\n");
  const actions = lines.filter((line) => /^  # [^(]/.test(line));
  const total = lines.find((line) => /^(Plan:|No changes\.)/.test(line)) ?? "No plan summary found.";
  const destroys = actions.filter((line) => /will be destroyed|must be replaced/.test(line));
  const warning = destroys.length > 0 ? `\n**${destroys.length} resource(s) will be destroyed or replaced.**\n` : "";

  return [
    `### Terraform plan: ${environment}`,
    warning,
    "```",
    ...actions,
    total,
    "```",
  ].join("\n");
};

if (require.main === module) {
  console.log(module.exports(process.argv[2], process.argv[3] ?? "local"));
}
