using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("uuid");

        builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(64);
        builder.Property(s => s.Date).IsRequired();
        builder.Property(s => s.CustomerId).HasColumnType("uuid");
        builder.Property(s => s.CustomerName).IsRequired().HasMaxLength(256);
        builder.Property(s => s.BranchId).HasColumnType("uuid");
        builder.Property(s => s.BranchName).IsRequired().HasMaxLength(256);
        builder.Property(s => s.IsCancelled).IsRequired();
        builder.Property(s => s.TotalAmount).HasPrecision(18, 2);
        builder.HasIndex(s => s.SaleNumber).IsUnique();
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.Date);

        builder
            .HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey("SaleId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
