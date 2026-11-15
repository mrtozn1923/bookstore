using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderInstitution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstitutionId",
                schema: "ordering",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_InstitutionId",
                schema: "ordering",
                table: "Orders",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Institutions_InstitutionId",
                schema: "ordering",
                table: "Orders",
                column: "InstitutionId",
                principalSchema: "institutional",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Institutions_InstitutionId",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_InstitutionId",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                schema: "ordering",
                table: "Orders");
        }
    }
}
