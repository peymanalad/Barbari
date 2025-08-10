using AutoMapper;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.Entities;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<CreateOrderDto, Order>();
        CreateMap<UpdateOrderDto, Order>();
    }
}