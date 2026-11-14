using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Update;

namespace Bookstore.Data.Migrations.Operations;

public class BookstoreMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    ICommandBatchPreparer commandBatchPreparer)
    : SqlServerMigrationsSqlGenerator(dependencies, commandBatchPreparer)
{
    protected override void Generate(MigrationOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        var sql = Dependencies.SqlGenerationHelper;
        switch (operation)
        {
            case CreateOrAlterViewOperation view:
                // CREATE VIEW must be alone in its batch; EXEC keeps idempotent scripts valid.
                var statement = $"CREATE OR ALTER VIEW {sql.DelimitIdentifier(view.Name, view.Schema)} AS {view.Definition}";
                builder
                    .Append("EXEC(N'")
                    .Append(statement.Replace("'", "''"))
                    .Append("')")
                    .AppendLine(sql.StatementTerminator)
                    .EndCommand();
                break;

            case DropViewOperation drop:
                builder
                    .Append("DROP VIEW IF EXISTS ")
                    .Append(sql.DelimitIdentifier(drop.Name, drop.Schema))
                    .AppendLine(sql.StatementTerminator)
                    .EndCommand();
                break;

            default:
                base.Generate(operation, model, builder);
                break;
        }
    }
}
