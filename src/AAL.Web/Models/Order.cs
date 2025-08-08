using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAL.Web.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public string CustomerId { get; set; } = string.Empty;
        public virtual Customer Customer { get; set; } = null!;

        public int? WarehouseId { get; set; }
        public virtual Warehouse? Warehouse { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? RequiredDate { get; set; }

        public DateTime? ShippedDate { get; set; }

        public DateTime? ConfirmedDate { get; set; }

        public DateTime? DeliveredDate { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public OrderPriority Priority { get; set; } = OrderPriority.Normal;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(1000)]
        public string? SpecialInstructions { get; set; }

        // Order Source
        public OrderSource Source { get; set; } = OrderSource.Web;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual Invoice? Invoice { get; set; }
    }

    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }

        [Required]
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Required]
        public int QuantityOrdered { get; set; }

        [NotMapped]
        public int Quantity => QuantityOrdered; // Alias for compatibility

        public int QuantityShipped { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; } // Made settable

        [StringLength(200)]
        public string? Notes { get; set; }
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Processing,
        PartiallyShipped,
        Shipped,
        Delivered,
        Cancelled,
        Rejected
    }

    public enum OrderPriority
    {
        Low,
        Normal,
        Medium,
        High,
        Urgent
    }

    public enum OrderSource
    {
        Web,
        Email,
        Fax,
        Phone,
        InPerson
    }
}
