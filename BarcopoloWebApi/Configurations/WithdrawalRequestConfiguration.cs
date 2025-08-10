using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.HasOne(wr => wr.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(wr => wr.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}