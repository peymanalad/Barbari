using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;

namespace BarcopoloWebApi.Data
{
    public class DataBaseContext : DbContext

    {
        public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options) { }

        public DbSet<Person> Persons { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationMembership> OrganizationMemberships { get; set; }
        public DbSet<CargoType> CargoTypes { get; set; }
        public DbSet<OrganizationCargoType> OrganizationCargoTypes { get; set; }
        public DbSet<SubOrganization> SubOrganizations { get; set; }
        public DbSet<UserToken> Tokens { get; set; }
        public DbSet<SmsCode> Sms { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<OrderEvent> OrderEvents { get; set; }
        public DbSet<Payment> Payments { get; set; }
        //public DbSet<Waybill> Waybills { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Bargir> Bargirs { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseVehicle> WarehouseVehicles { get; set; }
        public DbSet<OrderVehicle> OrderVehicles { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<CargoImage> CargoImages { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Person>(entity =>
            {
                entity.HasMany(p => p.Addresses)
                    .WithOne(a => a.Person)
                    .HasForeignKey(a => a.PersonId);
                entity.HasIndex(p => p.PhoneNumber)
                    .IsUnique();
            });


            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Memberships)
                .WithOne(m => m.Organization)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizationMembership>()
                .HasOne(m => m.Person)
                .WithMany()
                .HasForeignKey(m => m.PersonId);

            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Branches)
                .WithOne(s => s.Organization)
                .HasForeignKey(s => s.OrganizationId);

            modelBuilder.Entity<OrganizationCargoType>()
                .HasOne(oact => oact.Organization)
                .WithMany(o => o.AllowedCargoTypes)
                .HasForeignKey(oact => oact.OrganizationId);

            modelBuilder.Entity<OrganizationMembership>()
                .HasOne(m => m.Branch)
                .WithMany(s => s.Memberships)
                .HasForeignKey(m => m.BranchId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Cargos)
                .WithOne(c => c.Order)
                .HasForeignKey(c => c.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Events)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Payments)
                .WithOne(p => p.Order)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Feedback)
                .WithOne(f => f.Order)
                .HasForeignKey<Feedback>(f => f.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Driver)
                .WithMany(d => d.Vehicles)
                .HasForeignKey(v => v.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bargir>()
                .HasOne(b => b.Vehicle)
                .WithOne(v => v.Bargir)
                .HasForeignKey<Bargir>(b => b.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.Address)
                .WithMany() 
                .HasForeignKey(w => w.AddressId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseVehicle>()
                .HasKey(wv => new { wv.WarehouseId, wv.VehicleId });
            modelBuilder.Entity<WarehouseVehicle>()
                .HasOne(wv => wv.Warehouse)
                .WithMany(w => w.WarehouseVehicles)
                .HasForeignKey(wv => wv.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WarehouseVehicle>()
                .HasOne(wv => wv.Vehicle)
                .WithMany(v => v.WarehouseVehicles)
                .HasForeignKey(wv => wv.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderVehicle>()
                .HasKey(ov => new { ov.OrderId, ov.VehicleId });

            modelBuilder.Entity<OrderVehicle>()
                .HasOne(ov => ov.Order)
                .WithMany(o => o.OrderVehicles)
                .HasForeignKey(ov => ov.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderVehicle>()
                .HasOne(ov => ov.Vehicle)
                .WithMany(v => v.OrderVehicles)
                .HasForeignKey(ov => ov.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Warehouse)
                .WithMany()
                .HasForeignKey(o => o.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Driver>()
                .HasOne(d => d.Person)
                .WithOne(p => p.Driver)
                .HasForeignKey<Driver>(d => d.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Driver)
                .WithMany()
                .HasForeignKey(v => v.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Bargir>()
                .HasOne(b => b.Vehicle)
                .WithOne(v => v.Bargir)
                .HasForeignKey<Bargir>(b => b.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cargo>(entity =>
            {
                entity.HasMany(c => c.Images) 
                    .WithOne(ci => ci.Cargo)
                    .HasForeignKey(ci => ci.CargoId) 
                    .OnDelete(DeleteBehavior.Cascade);

   
                entity.HasOne(c => c.CargoType)
                    .WithMany()
                    .HasForeignKey(c => c.CargoTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Collector)
                .WithMany()
                .HasForeignKey(o => o.CollectorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Deliverer)
                .WithMany()
                .HasForeignKey(o => o.DelivererId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.FinalReceiver)
                .WithMany()
                .HasForeignKey(o => o.FinalReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Person>()
                .Property(p => p.Role)
                .HasConversion<string>();


            modelBuilder.Entity<SubOrganization>()
                .HasOne(s => s.Organization)
                .WithMany(o => o.Branches)
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SubOrganization>()
                .HasOne(s => s.OriginAddress)
                .WithMany()
                .HasForeignKey(s => s.OriginAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.OriginAddress)
                .WithMany()
                .HasForeignKey(o => o.OriginAddressId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.DestinationAddress)
                .WithMany()
                .HasForeignKey(o => o.DestinationAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cargo>()
                .HasOne(c => c.Owner)
                .WithMany(p => p.OwnedCargos)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WithdrawalRequest>()
                .HasOne(wr => wr.ReviewedByAdmin)
                .WithMany()
                .HasForeignKey(wr => wr.ReviewedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.OwnerOrganization)
                .WithOne(o => o.OrganizationWallet)
                .HasForeignKey<Organization>(o => o.OrganizationWalletId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.OwnerBranch)
                .WithOne(b => b.BranchWallet)
                .HasForeignKey<SubOrganization>(b => b.BranchWalletId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.OwnerPerson)
                .WithOne(p => p.PersonalWallet)
                .HasForeignKey<Person>(p => p.PersonalWalletId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);


            modelBuilder.Entity<Wallet>()
                .HasIndex(w => new { w.OwnerType, w.OwnerId })
                .IsUnique();
            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasPrecision(18, 2);



        }
    }
}