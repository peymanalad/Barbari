using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.DTOs.Bargir
{
    public class CreateBargirDto
    {
        [Required(ErrorMessage = "نام بارگیر الزامی است.")]
        public string Name { get; set; }

        [Range(0, float.MaxValue, ErrorMessage = "ظرفیت حداقل باید مثبت باشد.")]
        public float MinCapacity { get; set; }

        [Range(0, float.MaxValue, ErrorMessage = "ظرفیت حداکثر باید مثبت باشد.")]
        public float MaxCapacity { get; set; }
        public long? VehicleId { get; set; }
    }
}