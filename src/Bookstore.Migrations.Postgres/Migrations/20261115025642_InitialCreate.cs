using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bookstore.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.EnsureSchema(
                name: "ordering");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "Authors",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Isbn = table.Column<string>(type: "character varying(13)", unicode: false, maxLength: 13, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PublishedYear = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Books_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "catalog",
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Books_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlacedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", unicode: false, maxLength: 3, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "ordering",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Books_BookId",
                        column: x => x.BookId,
                        principalSchema: "catalog",
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalSchema: "ordering",
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockItems",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false),
                    QuantityReserved = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockItems", x => x.Id);
                    // Hand-edited: the model uses SQL Server [brackets]; PostgreSQL needs "double quotes".
                    table.CheckConstraint("CK_StockItems_Quantities", "\"QuantityReserved\" >= 0 AND \"QuantityReserved\" <= \"QuantityOnHand\"");
                    table.ForeignKey(
                        name: "FK_StockItems_Books_BookId",
                        column: x => x.BookId,
                        principalSchema: "catalog",
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    BookId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Books_BookId",
                        column: x => x.BookId,
                        principalSchema: "catalog",
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ordering",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Authors",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Frank Herbert" },
                    { 2, "J.R.R. Tolkien" },
                    { 3, "Jane Austen" },
                    { 4, "George Eliot" },
                    { 5, "George Orwell" },
                    { 6, "Aldous Huxley" },
                    { 7, "Mary Shelley" },
                    { 8, "Herman Melville" },
                    { 9, "Charlotte Brontë" },
                    { 10, "Emily Brontë" },
                    { 11, "Charles Dickens" },
                    { 12, "Fyodor Dostoevsky" },
                    { 13, "Leo Tolstoy" },
                    { 14, "F. Scott Fitzgerald" },
                    { 15, "Isaac Asimov" },
                    { 16, "Ursula K. Le Guin" },
                    { 17, "Alexandre Dumas" },
                    { 18, "Miguel de Cervantes" }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Categories",
                columns: new[] { "Id", "Name", "Slug" },
                values: new object[,]
                {
                    { 1, "Classics", "classics" },
                    { 2, "Science Fiction", "science-fiction" },
                    { 3, "Fantasy", "fantasy" },
                    { 4, "Dystopian", "dystopian" }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Books",
                columns: new[] { "Id", "AuthorId", "CategoryId", "Description", "Isbn", "Price", "PublishedYear", "Title" },
                values: new object[,]
                {
                    { 1, 1, 2, "A desert planet, a noble family and the most valuable substance in the universe.", "9780441172719", 489.00m, 1965, "Dune" },
                    { 2, 2, 3, "Bilbo Baggins leaves his comfortable home for an adventure with thirteen dwarves.", "9780261102217", 329.90m, 1937, "The Hobbit" },
                    { 3, 3, 1, "A young woman in a small English village keeps arranging other people's marriages.", "9780141439587", 249.90m, 1815, "Emma" },
                    { 4, 4, 1, "Several intertwined lives in a provincial English town during a time of reform.", "9780141439549", 389.00m, 1871, "Middlemarch" },
                    { 5, 3, 1, "Elizabeth Bennet and Mr Darcy misjudge each other, then slowly change their minds.", "9780141439518", 239.90m, 1813, "Pride and Prejudice" },
                    { 6, 5, 4, "Winston Smith works for a state that rewrites history and watches everyone.", "9780451524935", 279.00m, 1949, "1984" },
                    { 7, 6, 4, "A society kept stable by engineered birth, conditioning and constant pleasure.", "9780060850524", 299.00m, 1932, "Brave New World" },
                    { 8, 7, 1, "A scientist creates life and then abandons what he has made.", "9780141439471", 219.90m, 1818, "Frankenstein" },
                    { 9, 8, 1, "Captain Ahab hunts the white whale that took his leg.", "9780142437247", 359.00m, 1851, "Moby-Dick" },
                    { 10, 9, 1, "An orphan becomes a governess and refuses to give up her independence.", "9780141441146", 269.90m, 1847, "Jane Eyre" },
                    { 11, 10, 1, "A destructive love story on the Yorkshire moors across two generations.", "9780141439556", 249.00m, 1847, "Wuthering Heights" },
                    { 12, 11, 1, "Pip, an orphan, receives a fortune from an unknown benefactor.", "9780141439563", 289.00m, 1861, "Great Expectations" },
                    { 13, 12, 1, "A poor student commits a murder and cannot escape his own conscience.", "9780143058144", 349.00m, 1866, "Crime and Punishment" },
                    { 14, 13, 1, "A married woman's affair collides with the rules of Russian high society.", "9780143035008", 429.00m, 1878, "Anna Karenina" },
                    { 15, 13, 1, "Russian families live through the Napoleonic Wars.", "9781400079988", 689.00m, 1869, "War and Peace" },
                    { 16, 14, 1, "A mysterious millionaire throws parties to win back a lost love.", "9780743273565", 229.00m, 1925, "The Great Gatsby" },
                    { 17, 15, 2, "A mathematician predicts the fall of a galactic empire and plans for what comes after.", "9780553293357", 319.00m, 1951, "Foundation" },
                    { 18, 16, 2, "An envoy visits a planet whose people have no fixed gender.", "9780441478125", 309.00m, 1969, "The Left Hand of Darkness" },
                    { 19, 17, 1, "A wrongly imprisoned sailor escapes and plans his revenge.", "9780140449266", 459.00m, 1844, "The Count of Monte Cristo" },
                    { 20, 18, 1, "A man who has read too many chivalric romances sets out to become a knight.", "9780060934347", 399.00m, 1605, "Don Quixote" }
                });

            migrationBuilder.InsertData(
                schema: "inventory",
                table: "StockItems",
                columns: new[] { "Id", "BookId", "QuantityOnHand", "QuantityReserved" },
                values: new object[,]
                {
                    { 1, 1, 25, 0 },
                    { 2, 2, 25, 0 },
                    { 3, 3, 25, 0 },
                    { 4, 4, 25, 0 },
                    { 5, 5, 25, 0 },
                    { 6, 6, 25, 0 },
                    { 7, 7, 25, 0 },
                    { 8, 8, 25, 0 },
                    { 9, 9, 25, 0 },
                    { 10, 10, 25, 0 },
                    { 11, 11, 25, 0 },
                    { 12, 12, 25, 0 },
                    { 13, 13, 25, 0 },
                    { 14, 14, 25, 0 },
                    { 15, 15, 3, 0 },
                    { 16, 16, 25, 0 },
                    { 17, 17, 25, 0 },
                    { 18, 18, 25, 0 },
                    { 19, 19, 25, 0 },
                    { 20, 20, 0, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_AuthorId",
                schema: "catalog",
                table: "Books",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_CategoryId",
                schema: "catalog",
                table: "Books",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Isbn",
                schema: "catalog",
                table: "Books",
                column: "Isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_BookId",
                schema: "ordering",
                table: "CartItems",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_BookId",
                schema: "ordering",
                table: "CartItems",
                columns: new[] { "CartId", "BookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                schema: "ordering",
                table: "Customers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_BookId",
                schema: "ordering",
                table: "OrderItems",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "ordering",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "ordering",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_BookId",
                schema: "inventory",
                table: "StockItems",
                column: "BookId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "StockItems",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "Carts",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "Books",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "Authors",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "catalog");
        }
    }
}
