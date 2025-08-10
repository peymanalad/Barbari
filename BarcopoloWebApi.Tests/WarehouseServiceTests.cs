using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using BarcopoloWebApi.Data;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.Enums;
using BarcopoloWebApi.Services;
using BarcopoloWebApi.DTOs.Warehouse;

namespace BarcopoloWebApi.Tests
{
    public class WarehouseServiceTests
    {
        private static DataBaseContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<DataBaseContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new DataBaseContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static Person CreateAdmin()
        {
            return new Person
            {
                Id = 1,
                FirstName = "Admin",
                LastName = "User",
                PhoneNumber = "123",
                PasswordHash = "hash",
                Role = SystemRole.admin
            };
        }

        private static Address CreateAddress(long id = 1)
        {
            return new Address
            {
                Id = id,
                City = "City",
                Province = "Province",
                FullAddress = "Address",
                Plate = "25",
                Unit = "11",
                PostalCode = "123456789",
                Title = "Home"
            };
        }

        private static WarehouseService CreateService(DataBaseContext context)
            => new WarehouseService(context, NullLogger<WarehouseService>.Instance);

        [Fact]
        public async Task CreateWarehouse_AddsWarehouse()
        {
            using var context = CreateContext();
            context.Persons.Add(CreateAdmin());
            context.Addresses.Add(CreateAddress());
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new CreateWarehouseDto
            {
                WarehouseName = "Main",
                AddressId = 1,
                InternalTelephone = "111",
                ManagerPercentage = 10,
                Rent = 100,
                TerminalPercentage = 5,
                VatPercentage = 9,
                InsuranceAmount = 100,
                IsActive = true,
                IsCargoValueMandatory = false,
                IsDriverNetMandatory = false
            };

            var result = await service.CreateAsync(dto, 1);

            Assert.NotEqual(0, result.Id);
            Assert.Equal("Main", result.WarehouseName);
            Assert.Single(context.Warehouses);
        }

        [Fact]
        public async Task GetById_ReturnsWarehouse()
        {
            using var context = CreateContext();
            context.Persons.Add(CreateAdmin());
            context.Addresses.Add(CreateAddress());
            var warehouse = new Warehouse { AddressId = 1, WarehouseName = "Main", InternalTelephone = "123" ,PrintText = "warehouse"};
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var result = await service.GetByIdAsync(warehouse.Id, 1);

            Assert.Equal(warehouse.WarehouseName, result.WarehouseName);
            Assert.Equal(warehouse.Id, result.Id);
        }

        [Fact]
        public async Task UpdateWarehouse_ChangesValues()
        {
            using var context = CreateContext();
            context.Persons.Add(CreateAdmin());
            context.Addresses.Add(CreateAddress());
            var warehouse = new Warehouse { AddressId = 1, WarehouseName = "Main",InternalTelephone = "123", PrintText = "warehouse"};
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = new UpdateWarehouseDto { WarehouseName = "Updated" };

            var result = await service.UpdateAsync(warehouse.Id, dto, 1);

            Assert.Equal("Updated", result.WarehouseName);
            Assert.Equal("Updated", (await context.Warehouses.FindAsync(warehouse.Id))?.WarehouseName);
        }

        [Fact]
        public async Task DeleteWarehouse_RemovesWarehouse()
        {
            using var context = CreateContext();
            context.Persons.Add(CreateAdmin());
            context.Addresses.Add(CreateAddress());
            var warehouse = new Warehouse { AddressId = 1, WarehouseName = "Main", InternalTelephone = "123", PrintText = "warehouse" };
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            var deleted = await service.DeleteAsync(warehouse.Id, 1);

            Assert.True(deleted);
            Assert.Empty(context.Warehouses);
        }
    }
}
