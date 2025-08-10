using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class WarehouseVehicleConfiguration : IEntityTypeConfiguration<WarehouseVehicle>
{
    public void Configure(EntityTypeBuilder<WarehouseVehicle> builder)
    {
        builder.HasKey(wv => new { wv.WarehouseId, wv.VehicleId });

        builder.HasOne(wv => wv.Warehouse)
            .WithMany(w => w.WarehouseVehicles)
            .HasForeignKey(wv => wv.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wv => wv.Vehicle)
            .WithMany(v => v.WarehouseVehicles)
            .HasForeignKey(wv => wv.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}