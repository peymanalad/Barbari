using BarcopoloWebApi.Entities;
using BarcopoloWebApi.DTOs.Cargo;

namespace BarcopoloWebApi.Mappers
{
    public static class CargoMappingExtensions
    {
        public static CargoDto MapToDto(this Cargo cargo)
        {
            return new CargoDto
            {
                Id = cargo.Id,
                Title = cargo.Title,
                Contents = cargo.Contents,
                Value = cargo.Value,
                Weight = cargo.Weight,
                Length = cargo.Length,
                Width = cargo.Width,
                Height = cargo.Height,
                PackagingType = cargo.PackagingType,
                PackageCount = cargo.PackageCount,
                Description = cargo.Description,
                NeedsPackaging = cargo.NeedsPackaging,
                CargoTypeName = cargo.CargoType?.Name ?? "نامشخص",
                ImageUrls = cargo.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
            };
        }
    }
}