using AutoMapper;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.Vehicle;
using BarcopoloWebApi.DTOs.Warehouse;
using BarcopoloWebApi.Entities;

public class VehicleProfile : Profile
{
    public VehicleProfile()
    {
        CreateMap<Vehicle, VehicleDto>();
    }
}