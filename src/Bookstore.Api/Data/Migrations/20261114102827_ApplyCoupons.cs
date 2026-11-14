using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Data.Migrations
{
    /// <inheritdoc />
    public partial class ApplyCoupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                schema: "ordering",
                table: "Orders",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                schema: "ordering",
                table: "Orders",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                schema: "ordering",
                table: "Orders",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                schema: "ordering",
                table: "Carts",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CouponCode",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Discount",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CouponCode",
                schema: "ordering",
                table: "Carts");
        }
    }
}
