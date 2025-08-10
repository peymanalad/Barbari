using AutoMapper;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Entities;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        CreateMap<Payment, PaymentDto>();
    }
}