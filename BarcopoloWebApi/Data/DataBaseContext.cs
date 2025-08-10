using System.Reflection;
using BarcopoloWebApi.Entities;
using Microsoft.EntityFrameworkCore;
namespace BarcopoloWebApi.Data;

public class DataBaseContext : DbContext
{
    public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options)
    {
    }

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
    public DbSet<FrequentAddress> FrequentAddresses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataBaseContext).Assembly);
    }
}