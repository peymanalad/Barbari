using AutoMapper;
using BarcopoloWebApi.Entities;
using BarcopoloWebApi.DTOs.Cargo;

public class CargoProfile : Profile
{
    public CargoProfile()
    {
        CreateMap<Cargo, CargoDto>();
        CreateMap<CreateCargoDto, Cargo>();
    }
}