using AutoMapper;
using BarcopoloWebApi.DTOs.Order;
using BarcopoloWebApi.DTOs.Organization;
using BarcopoloWebApi.DTOs.Warehouse;
using BarcopoloWebApi.Entities;

public class OrganizationProfile : Profile
{
    public OrganizationProfile()
    {
        CreateMap<Organization, OrganizationDto>();
    }
}