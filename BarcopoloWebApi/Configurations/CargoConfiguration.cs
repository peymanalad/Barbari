using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class CargoConfiguration : IEntityTypeConfiguration<Cargo>
{
    public void Configure(EntityTypeBuilder<Cargo> builder)
    {
        builder.HasMany(c => c.Images)
            .WithOne(i => i.Cargo)
            .HasForeignKey(i => i.CargoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.CargoType)
            .WithMany()
            .HasForeignKey(c => c.CargoTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Owner)
            .WithMany(p => p.OwnedCargos)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Order)
            .WithMany(o => o.Cargos)
            .HasForeignKey(c => c.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}