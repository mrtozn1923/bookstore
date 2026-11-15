using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bookstore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "institutional");

            migrationBuilder.CreateTable(
                name: "Institutions",
                schema: "institutional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionUnits",
                schema: "institutional",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    ParentUnitId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstitutionUnits_InstitutionUnits_ParentUnitId",
                        column: x => x.ParentUnitId,
                        principalSchema: "institutional",
                        principalTable: "InstitutionUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstitutionUnits_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalSchema: "institutional",
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "institutional",
                table: "Institutions",
                columns: new[] { "Id", "Code", "Kind", "Name" },
                values: new object[,]
                {
                    { 1, "RHS", "School", "Riverside High School" },
                    { 2, "CCL", "Library", "Central City Library" }
                });

            migrationBuilder.InsertData(
                schema: "institutional",
                table: "InstitutionUnits",
                columns: new[] { "Id", "InstitutionId", "Name", "ParentUnitId" },
                values: new object[,]
                {
                    { 1, 1, "North Campus", null },
                    { 2, 1, "South Campus", null },
                    { 4, 2, "Main Branch", null },
                    { 3, 1, "Science Department", 1 },
                    { 5, 2, "Children's Section", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Code",
                schema: "institutional",
                table: "Institutions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionUnits_InstitutionId",
                schema: "institutional",
                table: "InstitutionUnits",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionUnits_ParentUnitId",
                schema: "institutional",
                table: "InstitutionUnits",
                column: "ParentUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstitutionUnits",
                schema: "institutional");

            migrationBuilder.DropTable(
                name: "Institutions",
                schema: "institutional");
        }
    }
}
