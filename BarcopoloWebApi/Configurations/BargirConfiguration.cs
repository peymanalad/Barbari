using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class BargirConfiguration : IEntityTypeConfiguration<Bargir>
{
    public void Configure(EntityTypeBuilder<Bargir> builder)
    {
        builder.HasOne(b => b.Vehicle)
            .WithOne(v => v.Bargir)
            .HasForeignKey<Bargir>(b => b.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}