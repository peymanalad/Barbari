using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class FrequentAddressConfiguration : IEntityTypeConfiguration<FrequentAddress>
{
    public void Configure(EntityTypeBuilder<FrequentAddress> builder)
    {
        builder.HasOne(f => f.Person)
            .WithMany()
            .HasForeignKey(f => f.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Organization)
            .WithMany()
            .HasForeignKey(f => f.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Branch)
            .WithMany()
            .HasForeignKey(f => f.BranchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.FullAddress, f.PersonId, f.OrganizationId, f.BranchId })
            .IsUnique(false);

        builder.Property(f => f.Title).HasMaxLength(100).IsRequired();
        builder.Property(f => f.FullAddress).HasMaxLength(1000).IsRequired();
    }
}