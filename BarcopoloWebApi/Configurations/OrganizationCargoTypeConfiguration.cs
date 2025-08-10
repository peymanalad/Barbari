using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class OrganizationCargoTypeConfiguration : IEntityTypeConfiguration<OrganizationCargoType>
{
    public void Configure(EntityTypeBuilder<OrganizationCargoType> builder)
    {
        builder.HasOne(oct => oct.Organization)
            .WithMany(o => o.AllowedCargoTypes)
            .HasForeignKey(oct => oct.OrganizationId);
    }
}