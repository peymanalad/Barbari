using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Collector)
            .WithMany()
            .HasForeignKey(o => o.CollectorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Deliverer)
            .WithMany()
            .HasForeignKey(o => o.DelivererId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.FinalReceiver)
            .WithMany()
            .HasForeignKey(o => o.FinalReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.OriginAddress)
            .WithMany()
            .HasForeignKey(o => o.OriginAddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}