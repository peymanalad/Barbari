using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasOne(f => f.Order)
            .WithOne(o => o.Feedback)
            .HasForeignKey<Feedback>(f => f.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}