using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class OrderVehicleConfiguration : IEntityTypeConfiguration<OrderVehicle>
{
    public void Configure(EntityTypeBuilder<OrderVehicle> builder)
    {
        builder.HasKey(ov => new { ov.OrderId, ov.VehicleId });

        builder.HasOne(ov => ov.Order)
            .WithMany(o => o.OrderVehicles)
            .HasForeignKey(ov => ov.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ov => ov.Vehicle)
            .WithMany(v => v.OrderVehicles)
            .HasForeignKey(ov => ov.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}