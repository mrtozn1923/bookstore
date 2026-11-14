using System;
using System.Collections.Generic;
using Bookstore.PgLab.Catalog;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Bookstore.PgLab.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.book_format", "audiobook,ebook,hardcover,paperback");

            migrationBuilder.CreateTable(
                name: "books",
                schema: "catalog",
                columns: table => new
                {
                    book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    isbn = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    format = table.Column<BookFormat>(type: "catalog.book_format", nullable: false),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "title" }),
                    details = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_books", x => x.book_id);
                    table.UniqueConstraint("ak_books_public_id", x => x.public_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_books_isbn",
                schema: "catalog",
                table: "books",
                column: "isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_books_tags",
                schema: "catalog",
                table: "books",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            // the application role gets DML on everything the owner creates in the catalog schema
            migrationBuilder.Sql("GRANT USAGE ON SCHEMA catalog TO bookstore_app;");
            migrationBuilder.Sql("GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA catalog TO bookstore_app;");
            migrationBuilder.Sql("GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA catalog TO bookstore_app;");
            migrationBuilder.Sql("ALTER DEFAULT PRIVILEGES IN SCHEMA catalog GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO bookstore_app;");
            migrationBuilder.Sql("ALTER DEFAULT PRIVILEGES IN SCHEMA catalog GRANT USAGE, SELECT ON SEQUENCES TO bookstore_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "books",
                schema: "catalog");
        }
    }
}
