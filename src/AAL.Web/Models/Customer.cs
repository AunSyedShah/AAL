using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AAL.Web.Models
{
    // Customer extending ASP.NET Identity User for authentication
    public class Customer : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        // Customer Rating System
        public CustomerRating Rating { get; set; } = CustomerRating.Regular;

        // Financial Information
        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingBalance { get; set; } = 0;

        // Customer Type
        public CustomerType CustomerType { get; set; } = CustomerType.Regular;

        // Payment History
        public int TotalOrders { get; set; } = 0;
        public int DefaultedPayments { get; set; } = 0;
        public DateTime LastOrderDate { get; set; }

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public enum CustomerRating
    {
        Regular,
        Silver,
        Gold,
        Platinum,
        Preferred,
        Premium,
        VIP
    }

    public enum CustomerType
    {
        Regular,
        Exclusive,
        OEM
    }
}
