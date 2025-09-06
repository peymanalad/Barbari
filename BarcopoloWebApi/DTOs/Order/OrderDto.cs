using BarcopoloWebApi.DTOs.Cargo;
using BarcopoloWebApi.DTOs.OrderEvent;
using BarcopoloWebApi.DTOs.Payment;
using BarcopoloWebApi.Helper;
using Newtonsoft.Json;

namespace BarcopoloWebApi.DTOs.Order
{
    public class OrderDto
    {
        public long Id { get; set; }

        public string TrackingNumber { get; set; }
        public string Status { get; set; }

        public string OriginAddress { get; set; }
        public string DestinationAddress { get; set; }

        public string SenderName { get; set; }
        public string SenderPhone { get; set; }

        public string ReceiverName { get; set; }
        public string ReceiverPhone { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal Fare { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal Insurance { get; set; }
        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal Vat { get; set; }

        [JsonConverter(typeof(CurrencyDecimalConverter))]
        public decimal TotalCost => Fare + Insurance + Vat;

        public DateTime? LoadingTime { get; set; }
        public DateTime? DeliveryTime { get; set; }

        public string Description { get; set; }

        public List<string> AssignedVehiclePlates { get; set; } = new();
        public List<CargoDto> Cargos { get; set; } = new();
        public List<PaymentDto> Payments { get; set; } = new();
        public List<OrderEventDto> Events { get; set; } = new();


        public string? WarehouseName { get; set; }
        public string? OrganizationName { get; set; }
    }
}