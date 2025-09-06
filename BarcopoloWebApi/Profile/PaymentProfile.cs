using AutoMapper;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Entities;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.PaymentType, opt => opt.MapFrom(src => src.PaymentMethod));
    }
}