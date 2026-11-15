using System.CommandLine;
using Bookstore.Admin.Pricing;
using Bookstore.Bcl.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Admin;

public static class AdminCommands
{
    public static RootCommand Create(IServiceProvider services)
    {
        RootCommand root = new("Bookstore admin tool. Exit codes: 0 success, 1 usage error, 2 invalid data, 130 canceled.");
        root.Subcommands.Add(CreateImportPrices(services));
        root.Subcommands.Add(CreateCheckIsbn());
        return root;
    }

    private static Command CreateImportPrices(IServiceProvider services)
    {
        Argument<FileInfo> file = new("file") { Description = "CSV file with isbn,price rows" };
        file.AcceptExistingOnly();

        Option<bool> dryRun = new("--dry-run", "-n") { Description = "Validate only; do not change any price" };
        Option<int> maxErrors = new("--max-errors")
        {
            Description = "Stop reading after this many invalid rows",
            DefaultValueFactory = _ => 10,
        };
        Option<FileInfo> report = new("--report")
        {
            Description = "Where to write the applied changes",
            DefaultValueFactory = _ => new FileInfo("import-report.csv"),
        };

        Command command = new("import-prices", "Import new book prices from a CSV file") { file, dryRun, maxErrors, report };
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            FileInfo input = parseResult.GetValue(file)!;
            bool isDryRun = parseResult.GetValue(dryRun);
            PriceImporter importer = services.GetRequiredService<PriceImporter>();

            try
            {
                ImportResult result = await importer.ImportAsync(input.FullName, isDryRun, parseResult.GetValue(maxErrors),
                    parseResult.GetValue(report)!.FullName, cancellationToken);

                foreach (string error in result.Errors)
                    ConsoleOutput.Error(error);
                if (result.Errors.Count > 0)
                {
                    ConsoleOutput.Error($"{result.Errors.Count} problem(s) in {input.Name}; no price was changed");
                    return ExitCodes.InvalidData;
                }

                ConsoleOutput.Success(isDryRun
                    ? $"dry run: {result.ValidRows.Count} of {result.RowsRead} rows are valid; nothing was changed"
                    : $"updated {result.Applied} prices; report: {parseResult.GetValue(report)!.Name}");
                return ExitCodes.Success;
            }
            catch (OperationCanceledException)
            {
                ConsoleOutput.Warning("canceled; the report was not completed");
                return ExitCodes.Canceled;
            }
        });
        return command;
    }

    private static Command CreateCheckIsbn()
    {
        Argument<string[]> isbns = new("isbn") { Description = "One or more ISBN-13 values", Arity = ArgumentArity.OneOrMore };
        Command command = new("check-isbn", "Normalize and validate ISBN-13 values") { isbns };
        command.SetAction(parseResult =>
        {
            int invalid = 0;
            foreach (string value in parseResult.GetValue(isbns)!)
            {
                if (Isbn.TryParse(value, out Isbn isbn))
                    Console.WriteLine($"{value} -> {isbn:P}");
                else
                {
                    ConsoleOutput.Error($"'{value}' is not a valid ISBN-13");
                    invalid++;
                }
            }
            return invalid == 0 ? ExitCodes.Success : ExitCodes.InvalidData;
        });
        return command;
    }
}
