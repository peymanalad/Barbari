using BarcopoloWebApi.Enums;
using System.ComponentModel.DataAnnotations;

namespace BarcopoloWebApi.Entities
{
    public class Order
    {
        public long Id { get; set; }

        [Required]
        public long OwnerId { get; set; }

        public long? OrganizationId { get; set; }
        public long? BranchId { get; set; }

        public long? CollectorId { get; set; }
        public long? DelivererId { get; set; }
        public long? FinalReceiverId { get; set; }

        [Required]
        public long OriginAddressId { get; set; }

        [Required]
        public long DestinationAddressId { get; set; }

        public long? WarehouseId { get; set; }

        public DateTime? LoadingTime { get; set; }
        public DateTime? DeliveryTime { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Details { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required, MaxLength(50)]
        public string TrackingNumber { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Fare { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Insurance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Vat { get; set; }

        [Required, MaxLength(100)]
        public string SenderName { get; set; }

        [Required, MaxLength(20)]
        public string SenderPhone { get; set; }

        [Required, MaxLength(100)]
        public string ReceiverName { get; set; }

        [Required, MaxLength(20)]
        public string ReceiverPhone { get; set; }

        [MaxLength(1000)]
        public string OrderDescription { get; set; }


        public virtual Person Owner { get; set; }
        public virtual Organization Organization { get; set; }
        public virtual SubOrganization Branch { get; set; }

        public virtual Person Collector { get; set; }
        public virtual Person Deliverer { get; set; }
        public virtual Person FinalReceiver { get; set; }

        public virtual Address OriginAddress { get; set; }
        public virtual Address DestinationAddress { get; set; }

        public virtual Warehouse Warehouse { get; set; }

        public virtual ICollection<OrderEvent> Events { get; set; } = new List<OrderEvent>();
        public virtual ICollection<Cargo> Cargos { get; set; } = new List<Cargo>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual Feedback Feedback { get; set; }
        public virtual ICollection<OrderVehicle> OrderVehicles { get; set; } = new List<OrderVehicle>();

        public bool IsCompleted() => Status == OrderStatus.Delivered;

        public void SetStatus(OrderStatus newStatus)
        {
            if ((int)newStatus < (int)Status)
                throw new InvalidOperationException("Cannot revert order to a previous status.");

            Status = newStatus;
        }

        public bool IsReadyForDelivery()
        {
            return Status == OrderStatus.Loading && DeliveryTime == null;
        }
    }
}
