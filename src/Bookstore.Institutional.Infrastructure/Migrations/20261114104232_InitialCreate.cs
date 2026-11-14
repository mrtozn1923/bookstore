using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookstore.Institutional.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitBudgetId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdjustedById = table.Column<int>(type: "int", nullable: false),
                    AdjustedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAdjustments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BudgetCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeductsFromBudget = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscountAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountAgreements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionUnitId = table.Column<int>(type: "int", nullable: false),
                    BudgetCategoryId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SpentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PurchasingManagerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InstitutionUnitId = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstitutionUsers_InstitutionUnits_InstitutionUnitId",
                        column: x => x.InstitutionUnitId,
                        principalTable: "InstitutionUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstitutionUsers_InstitutionUsers_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "InstitutionUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "42, 1"),
                    PlacedById = table.Column<int>(type: "int", nullable: false),
                    BudgetCategoryId = table.Column<int>(type: "int", nullable: false),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    DeliveryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Attachment_FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    Attachment_ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Attachment_StoragePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_BudgetCategories_BudgetCategoryId",
                        column: x => x.BudgetCategoryId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_InstitutionUsers_PlacedById",
                        column: x => x.PlacedById,
                        principalTable: "InstitutionUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseApprovals_InstitutionUsers_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "InstitutionUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseApprovals_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_Code",
                table: "BudgetCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionUnits_PurchasingManagerId",
                table: "InstitutionUnits",
                column: "PurchasingManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionUsers_ApproverId",
                table: "InstitutionUsers",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionUsers_InstitutionUnitId",
                table: "InstitutionUsers",
                column: "InstitutionUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionUsers_StaffNumber",
                table: "InstitutionUsers",
                column: "StaffNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BudgetCategoryId",
                table: "Orders",
                column: "BudgetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PlacedById",
                table: "Orders",
                column: "PlacedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseApprovals_ApproverId",
                table: "PurchaseApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseApprovals_OrderId_Step",
                table: "PurchaseApprovals",
                columns: new[] { "OrderId", "Step" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitBudgets_InstitutionUnitId_Year_BudgetCategoryId",
                table: "UnitBudgets",
                columns: new[] { "InstitutionUnitId", "Year", "BudgetCategoryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InstitutionUnits_InstitutionUsers_PurchasingManagerId",
                table: "InstitutionUnits",
                column: "PurchasingManagerId",
                principalTable: "InstitutionUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstitutionUnits_InstitutionUsers_PurchasingManagerId",
                table: "InstitutionUnits");

            migrationBuilder.DropTable(
                name: "BudgetAdjustments");

            migrationBuilder.DropTable(
                name: "DiscountAgreements");

            migrationBuilder.DropTable(
                name: "PurchaseApprovals");

            migrationBuilder.DropTable(
                name: "UnitBudgets");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "BudgetCategories");

            migrationBuilder.DropTable(
                name: "InstitutionUsers");

            migrationBuilder.DropTable(
                name: "InstitutionUnits");
        }
    }
}
