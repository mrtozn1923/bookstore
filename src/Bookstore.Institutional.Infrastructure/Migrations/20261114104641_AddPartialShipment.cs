using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Institutional.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinFirstShipment",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentPreference",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SingleShipment"); // mevcut siparişler tek sevkiyat
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinFirstShipment",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipmentPreference",
                table: "Orders");
        }
    }
}
