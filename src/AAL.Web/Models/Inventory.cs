using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAL.Web.Models
{
    public class Warehouse
    {
        [Key]
        public int WarehouseId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(20)]
        public string ContactPhone { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    public class InventoryItem
    {
        [Key]
        public int InventoryItemId { get; set; }

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Required]
        public int WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;

        [Required]
        public int QuantityInStock { get; set; }

        public int ReorderPoint { get; set; }

        public int EconomicOrderQuantity { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Inventory Classification
        public InventoryCategory Category { get; set; } = InventoryCategory.Regular;
        public MovementType MovementType { get; set; } = MovementType.Slow;
    }

    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // Convenience properties
        [NotMapped]
        public string Name => ProductName;
        [NotMapped]
        public decimal Price => UnitPrice;
        [NotMapped]
        public string SKU => ProductCode;

        [StringLength(50)]
        public string Unit { get; set; } = string.Empty; // pieces, kg, etc.

        public ProductCategory Category { get; set; } = ProductCategory.TwoWheeler;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<MaterialRejection> MaterialRejections { get; set; } = new List<MaterialRejection>();
    }

    public enum InventoryCategory
    {
        Regular,
        Seasonal
    }

    public enum MovementType
    {
        Fast,
        Slow
    }

    public enum ProductCategory
    {
        TwoWheeler,
        FourWheeler
    }
}
