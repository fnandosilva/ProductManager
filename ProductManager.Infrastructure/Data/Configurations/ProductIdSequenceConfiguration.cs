using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductManager.Domain.Entities;

namespace ProductManager.Infrastructure.Data.Configurations;

public class ProductIdSequenceConfiguration : IEntityTypeConfiguration<ProductIdSequence>
{
    public void Configure(EntityTypeBuilder<ProductIdSequence> builder)
    {
        builder.ToTable("ProductIdSequences");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.NextProductId)
            .IsRequired();
    }
}
