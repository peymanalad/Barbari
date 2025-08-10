using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class SubOrganizationConfiguration : IEntityTypeConfiguration<SubOrganization>
{
    public void Configure(EntityTypeBuilder<SubOrganization> builder)
    {
        builder.HasOne(s => s.Organization)
            .WithMany(o => o.Branches)
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}