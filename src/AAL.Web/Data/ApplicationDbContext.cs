using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Models;

namespace AAL.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<Customer>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all entities
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MaterialRejection> MaterialRejections { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Customer entity
            builder.Entity<Customer>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
                entity.Property(e => e.OutstandingBalance).HasPrecision(18, 2);
            });

            // Configure Warehouse entity
            builder.Entity<Warehouse>(entity =>
            {
                entity.HasKey(e => e.WarehouseId);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Product entity
            builder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.HasIndex(e => e.ProductCode).IsUnique();
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            });

            // Configure InventoryItem entity
            builder.Entity<InventoryItem>(entity =>
            {
                entity.HasKey(e => e.InventoryItemId);
                entity.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();
                
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.InventoryItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Warehouse)
                    .WithMany(w => w.InventoryItems)
                    .HasForeignKey(e => e.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Order entity
            builder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                
                entity.Property(e => e.SubTotal).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Warehouse)
                    .WithMany(w => w.Orders)
                    .HasForeignKey(e => e.WarehouseId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure OrderItem entity
            builder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                // LineTotal is NotMapped - removed configuration

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Invoice entity
            builder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.InvoiceId);
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.HasIndex(e => e.OrderId).IsUnique();
                
                entity.Property(e => e.SubTotal).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
                entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.TaxPercentage).HasPrecision(5, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.AmountPaid).HasPrecision(18, 2);
                entity.Property(e => e.OutstandingAmount).HasPrecision(18, 2);

                entity.HasOne(e => e.Order)
                    .WithOne(o => o.Invoice)
                    .HasForeignKey<Invoice>(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Invoices)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Payment entity
            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.HasIndex(e => e.PaymentNumber).IsUnique();
                entity.Property(e => e.Amount).HasPrecision(18, 2);

                entity.HasOne(e => e.Invoice)
                    .WithMany(i => i.Payments)
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Payments)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure MaterialRejection entity
            builder.Entity<MaterialRejection>(entity =>
            {
                entity.HasKey(e => e.RejectionId);
                entity.HasIndex(e => e.RejectionNumber).IsUnique();
                entity.Property(e => e.CostImpact).HasPrecision(18, 2);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.MaterialRejections)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Customer)
                    .WithMany()
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed initial data
            SeedData(builder);
        }

        private void SeedData(ModelBuilder builder)
        {
            // Seed Warehouses
            builder.Entity<Warehouse>().HasData(
                new Warehouse { WarehouseId = 1, Name = "Main Warehouse", Location = "Mumbai", Address = "123 Industrial Area, Mumbai", ContactPhone = "+91-22-12345678" },
                new Warehouse { WarehouseId = 2, Name = "North Warehouse", Location = "Delhi", Address = "456 Industrial Zone, Delhi", ContactPhone = "+91-11-87654321" },
                new Warehouse { WarehouseId = 3, Name = "South Warehouse", Location = "Chennai", Address = "789 Manufacturing Hub, Chennai", ContactPhone = "+91-44-11223344" },
                new Warehouse { WarehouseId = 4, Name = "West Warehouse", Location = "Pune", Address = "321 Auto Park, Pune", ContactPhone = "+91-20-55667788" }
            );

            // Seed Products
            builder.Entity<Product>().HasData(
                new Product { ProductId = 1, ProductCode = "TW001", ProductName = "Two-Wheeler Brake Pad", Description = "High-quality brake pad for motorcycles", UnitPrice = 250.00m, Unit = "piece", Category = ProductCategory.TwoWheeler },
                new Product { ProductId = 2, ProductCode = "TW002", ProductName = "Two-Wheeler Air Filter", Description = "Premium air filter for motorcycles", UnitPrice = 150.00m, Unit = "piece", Category = ProductCategory.TwoWheeler },
                new Product { ProductId = 3, ProductCode = "FW001", ProductName = "Four-Wheeler Brake Disc", Description = "Durable brake disc for cars", UnitPrice = 1500.00m, Unit = "piece", Category = ProductCategory.FourWheeler },
                new Product { ProductId = 4, ProductCode = "FW002", ProductName = "Four-Wheeler Oil Filter", Description = "Premium oil filter for cars", UnitPrice = 300.00m, Unit = "piece", Category = ProductCategory.FourWheeler },
                new Product { ProductId = 5, ProductCode = "TW003", ProductName = "Two-Wheeler Spark Plug", Description = "High-performance spark plug", UnitPrice = 80.00m, Unit = "piece", Category = ProductCategory.TwoWheeler }
            );

            // Seed Inventory Items
            builder.Entity<InventoryItem>().HasData(
                // Mumbai Warehouse
                new InventoryItem { InventoryItemId = 1, ProductId = 1, WarehouseId = 1, QuantityInStock = 500, ReorderPoint = 100, EconomicOrderQuantity = 200, Category = InventoryCategory.Regular, MovementType = MovementType.Fast },
                new InventoryItem { InventoryItemId = 2, ProductId = 2, WarehouseId = 1, QuantityInStock = 300, ReorderPoint = 50, EconomicOrderQuantity = 150, Category = InventoryCategory.Regular, MovementType = MovementType.Fast },
                new InventoryItem { InventoryItemId = 3, ProductId = 3, WarehouseId = 1, QuantityInStock = 200, ReorderPoint = 40, EconomicOrderQuantity = 100, Category = InventoryCategory.Regular, MovementType = MovementType.Slow },
                
                // Delhi Warehouse
                new InventoryItem { InventoryItemId = 4, ProductId = 1, WarehouseId = 2, QuantityInStock = 400, ReorderPoint = 80, EconomicOrderQuantity = 180, Category = InventoryCategory.Regular, MovementType = MovementType.Fast },
                new InventoryItem { InventoryItemId = 5, ProductId = 4, WarehouseId = 2, QuantityInStock = 250, ReorderPoint = 60, EconomicOrderQuantity = 120, Category = InventoryCategory.Regular, MovementType = MovementType.Fast },
                
                // Chennai Warehouse
                new InventoryItem { InventoryItemId = 6, ProductId = 2, WarehouseId = 3, QuantityInStock = 350, ReorderPoint = 70, EconomicOrderQuantity = 160, Category = InventoryCategory.Regular, MovementType = MovementType.Fast },
                new InventoryItem { InventoryItemId = 7, ProductId = 5, WarehouseId = 3, QuantityInStock = 600, ReorderPoint = 120, EconomicOrderQuantity = 250, Category = InventoryCategory.Regular, MovementType = MovementType.Fast },
                
                // Pune Warehouse
                new InventoryItem { InventoryItemId = 8, ProductId = 3, WarehouseId = 4, QuantityInStock = 150, ReorderPoint = 30, EconomicOrderQuantity = 80, Category = InventoryCategory.Regular, MovementType = MovementType.Slow },
                new InventoryItem { InventoryItemId = 9, ProductId = 4, WarehouseId = 4, QuantityInStock = 280, ReorderPoint = 55, EconomicOrderQuantity = 130, Category = InventoryCategory.Regular, MovementType = MovementType.Fast }
            );
        }
    }
}
