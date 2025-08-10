using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.HasOne(m => m.Person)
            .WithMany(p => p.Memberships)
            .HasForeignKey(m => m.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Branch)
            .WithMany(b => b.Memberships)
            .HasForeignKey(m => m.BranchId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}