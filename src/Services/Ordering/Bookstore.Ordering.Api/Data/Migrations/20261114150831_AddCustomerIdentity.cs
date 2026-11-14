using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Ordering.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Buyer_Id",
                table: "Orders",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "CheckoutSagas",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Buyer_Id",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "CheckoutSagas");
        }
    }
}
