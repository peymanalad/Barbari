using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BarcopoloWebApi.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasOne(w => w.OwnerOrganization)
            .WithOne(o => o.OrganizationWallet)
            .HasForeignKey<Organization>(o => o.OrganizationWalletId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.OwnerBranch)
            .WithOne(b => b.BranchWallet)
            .HasForeignKey<SubOrganization>(b => b.BranchWalletId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.OwnerPerson)
            .WithOne(p => p.PersonalWallet)
            .HasForeignKey<Person>(p => p.PersonalWalletId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(w => new { w.OwnerType, w.OwnerId }).IsUnique();
        builder.Property(w => w.Balance).HasPrecision(18, 2);
        builder.Property(w => w.RowVersion).IsRowVersion();
    }
}