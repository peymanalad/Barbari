using AutoMapper;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.Warehouse;
using BarcopoloWebApi.Entities;

public class WarehouseProfile : Profile
{
    public WarehouseProfile()
    {
        CreateMap<Warehouse, WarehouseDto>();
    }
}