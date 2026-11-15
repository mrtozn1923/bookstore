using Bookstore.Catalog.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bookstore.Catalog.Tests;

public sealed class BookConfigurationTests
{
    [Fact]
    public void Configure_BookEntity_UsesIsbnAsKeyAndBooksTable()
    {
        ModelBuilder modelBuilder = new();
        new BookConfiguration().Configure(modelBuilder.Entity<Book>());

        IMutableEntityType entity = modelBuilder.Model.FindEntityType(typeof(Book))!;
        Assert.Equal(["Isbn"], entity.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.Equal("Books", entity.GetTableName());
    }
}
