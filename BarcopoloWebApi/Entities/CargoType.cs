    using System.ComponentModel.DataAnnotations;

    namespace BarcopoloWebApi.Entities
    {
        public class CargoType
        {
            public long Id { get; set; }

            [Required, MaxLength(100)]
            public string Name { get; set; }


        }
    }