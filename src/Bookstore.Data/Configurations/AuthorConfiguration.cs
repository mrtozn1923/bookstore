using Bookstore.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookstore.Data.Configurations;

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> b)
    {
        b.ToTable("Authors", "catalog");
        b.Property(x => x.Id).UseHiLo("AuthorIds", "catalog");
        b.Property(x => x.Name).HasMaxLength(100);
    }
}
