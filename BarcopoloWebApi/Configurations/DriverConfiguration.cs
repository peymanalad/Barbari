using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> builder)
    {
        builder.HasOne(d => d.Person)
            .WithOne(p => p.Driver)
            .HasForeignKey<Driver>(d => d.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}