using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.CargoType
{
    public class UpdateCargoTypeDto
    {
        [Required(ErrorMessage = "نام نوع بار الزامی است.")]
        [MaxLength(100, ErrorMessage = "حداکثر طول نام 100 کاراکتر است.")]
        public string Name { get; set; }
    }
}