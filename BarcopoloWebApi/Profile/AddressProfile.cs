using AutoMapper;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.Entities;

public class AddressProfile : Profile
{
    public AddressProfile()
    {
        CreateMap<Address, AddressDto>();

    }
}