using AutoMapper;
using BarcopoloWebApi.DTOs.OrderEvent;
using BarcopoloWebApi.Entities;

public class OrderEventProfile : Profile
{
    public OrderEventProfile()
    {
        CreateMap<OrderEvent, OrderEventDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ChangedByFullName, opt => opt.Ignore()); // دستی تنظیم میشه
    }
}