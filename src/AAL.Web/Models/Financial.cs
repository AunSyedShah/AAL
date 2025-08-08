using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAL.Web.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(20)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        [Required]
        public string CustomerId { get; set; } = string.Empty;
        public virtual Customer Customer { get; set; } = null!;

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingAmount { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentNumber { get; set; } = string.Empty;

        [Required]
        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; } = null!;

        [Required]
        public string CustomerId { get; set; } = string.Empty;
        public virtual Customer Customer { get; set; } = null!;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class MaterialRejection
    {
        [Key]
        public int RejectionId { get; set; }

        [Required]
        [StringLength(20)]
        public string RejectionNumber { get; set; } = string.Empty;

        [Required]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        [Required]
        public string CustomerId { get; set; } = string.Empty;
        public virtual Customer Customer { get; set; } = null!;

        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public DateTime RejectionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int QuantityRejected { get; set; }

        [NotMapped]
        public int RejectedQuantity { get; set; } // Made settable for compatibility

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public RejectionStatus Status { get; set; } = RejectionStatus.Reported;

        public DateTime? ResolutionDate { get; set; }

        [StringLength(1000)]
        public string? ResolutionNotes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CostImpact { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public enum InvoiceStatus
    {
        Generated,
        Pending,
        Sent,
        PartiallyPaid,
        Paid,
        Overdue,
        Cancelled
    }

    public enum PaymentMethod
    {
        Cash,
        Cheque,
        BankTransfer,
        CreditCard,
        OnlinePayment
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Cancelled
    }

    public enum RejectionStatus
    {
        Reported,
        UnderInvestigation,
        Resolved,
        Closed
    }
}
