using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Bookstore.Data.Migrations.Operations;

public class CreateOrAlterViewOperation : MigrationOperation
{
    public required string Schema { get; init; }
    public required string Name { get; init; }
    public required string Definition { get; init; }
}

public class DropViewOperation : MigrationOperation
{
    public required string Schema { get; init; }
    public required string Name { get; init; }
}

public static class ViewMigrationBuilderExtensions
{
    public static OperationBuilder<CreateOrAlterViewOperation> CreateOrAlterView(
        this MigrationBuilder migrationBuilder, string schema, string name, string definition)
    {
        var operation = new CreateOrAlterViewOperation { Schema = schema, Name = name, Definition = definition };
        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<CreateOrAlterViewOperation>(operation);
    }

    public static OperationBuilder<DropViewOperation> DropView(
        this MigrationBuilder migrationBuilder, string schema, string name)
    {
        var operation = new DropViewOperation { Schema = schema, Name = name };
        migrationBuilder.Operations.Add(operation);
        return new OperationBuilder<DropViewOperation>(operation);
    }
}
