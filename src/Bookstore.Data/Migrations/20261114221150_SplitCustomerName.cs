using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitCustomerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // EXEC: in an idempotent script the new columns don't exist yet at compile time.
            migrationBuilder.Sql("""
                EXEC(N'
                UPDATE [Customers]
                SET [FirstName] = CASE WHEN CHARINDEX(N'' '', [FullName]) > 0
                                       THEN LEFT([FullName], CHARINDEX(N'' '', [FullName]) - 1)
                                       ELSE [FullName] END,
                    [LastName]  = CASE WHEN CHARINDEX(N'' '', [FullName]) > 0
                                       THEN LTRIM(SUBSTRING([FullName], CHARINDEX(N'' '', [FullName]) + 1, 200))
                                       ELSE N'''' END;
                ');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Customers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                EXEC(N'
                UPDATE [Customers]
                SET [FullName] = TRIM([FirstName] + N'' '' + [LastName]);
                ');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Customers");
        }
    }
}
