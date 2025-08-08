using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Pages
{
    [Authorize(Roles = "Admin,Manager")]
    public class ServerStatusModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ServerStatusModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Server Status Properties
        public int OnlineManufacturingPlants { get; set; }
        public int OfflineManufacturingPlants { get; set; }
        public int OnlineWarehouses { get; set; }
        public int OfflineWarehouses { get; set; }
        public int ConnectedClients { get; set; }

        // Performance Metrics
        public int AverageResponseTime { get; set; }
        public double SystemUptime { get; set; }
        public double DataTransferRate { get; set; }
        public int SyncFrequency { get; set; }

        // Server Collections
        public List<ManufacturingPlantStatus> ManufacturingPlants { get; set; } = new();
        public List<WarehouseServerStatus> WarehouseServers { get; set; } = new();
        public List<SyncOperationStatus> SyncStatus { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadServerStatusAsync();
            GenerateMockData();
        }

        private async Task LoadServerStatusAsync()
        {
            // Load actual warehouse data
            var warehouses = await _context.Warehouses
                .Where(w => w.IsActive)
                .ToListAsync();

            WarehouseServers = warehouses.Select(w => new WarehouseServerStatus
            {
                WarehouseId = w.WarehouseId,
                Name = w.Name,
                Location = w.Location,
                IsOnline = Random.Shared.Next(0, 10) > 1, // 90% uptime simulation
                StorageUtilization = Random.Shared.Next(45, 95),
                ActiveInventoryItems = Random.Shared.Next(50, 200),
                LastDataSync = DateTime.Now.AddMinutes(-Random.Shared.Next(0, 15))
            }).ToList();

            OnlineWarehouses = WarehouseServers.Count(w => w.IsOnline);
            OfflineWarehouses = WarehouseServers.Count(w => !w.IsOnline);
        }

        private void GenerateMockData()
        {
            // Manufacturing Plants (simulated as we don't have actual plant servers)
            ManufacturingPlants = new List<ManufacturingPlantStatus>
            {
                new() { PlantId = "MFG-001", Location = "Mumbai, Maharashtra", IsOnline = true, CapacityUtilization = 78, CurrentProduction = 1250, ConnectionQuality = "Excellent", LastSync = DateTime.Now.AddMinutes(-2) },
                new() { PlantId = "MFG-002", Location = "Chennai, Tamil Nadu", IsOnline = true, CapacityUtilization = 85, CurrentProduction = 1420, ConnectionQuality = "Good", LastSync = DateTime.Now.AddMinutes(-1) },
                new() { PlantId = "MFG-003", Location = "Pune, Maharashtra", IsOnline = false, CapacityUtilization = 0, CurrentProduction = 0, ConnectionQuality = "Poor", LastSync = DateTime.Now.AddHours(-2) },
                new() { PlantId = "MFG-004", Location = "Bangalore, Karnataka", IsOnline = true, CapacityUtilization = 92, CurrentProduction = 1680, ConnectionQuality = "Good", LastSync = DateTime.Now.AddMinutes(-3) }
            };

            OnlineManufacturingPlants = ManufacturingPlants.Count(p => p.IsOnline);
            OfflineManufacturingPlants = ManufacturingPlants.Count(p => !p.IsOnline);

            // Sync Status
            SyncStatus = new List<SyncOperationStatus>
            {
                new() { Operation = "Inventory Sync", Description = "Warehouse → Main Server", IsSuccessful = true, LastSync = DateTime.Now.AddMinutes(-5) },
                new() { Operation = "Production Data", Description = "Manufacturing Plants → Main Server", IsSuccessful = true, LastSync = DateTime.Now.AddMinutes(-3) },
                new() { Operation = "Order Processing", Description = "Customer Orders → Warehouses", IsSuccessful = true, LastSync = DateTime.Now.AddMinutes(-2) },
                new() { Operation = "Financial Data", Description = "Billing → Accounting System", IsSuccessful = false, LastSync = DateTime.Now.AddMinutes(-10) },
                new() { Operation = "Material Rejections", Description = "Quality Control → Main Server", IsSuccessful = true, LastSync = DateTime.Now.AddMinutes(-1) }
            };

            // Performance Metrics
            ConnectedClients = Random.Shared.Next(45, 75);
            AverageResponseTime = Random.Shared.Next(120, 450);
            SystemUptime = 0.9985; // 99.85% uptime
            DataTransferRate = Math.Round(Random.Shared.NextDouble() * 50 + 10, 1); // 10-60 MB/s
            SyncFrequency = 15; // Every 15 seconds
        }
    }

    public class ManufacturingPlantStatus
    {
        public string PlantId { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int CapacityUtilization { get; set; }
        public int CurrentProduction { get; set; }
        public string ConnectionQuality { get; set; } = string.Empty;
        public DateTime LastSync { get; set; }
    }

    public class WarehouseServerStatus
    {
        public int WarehouseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int StorageUtilization { get; set; }
        public int ActiveInventoryItems { get; set; }
        public DateTime LastDataSync { get; set; }
    }

    public class SyncOperationStatus
    {
        public string Operation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public DateTime LastSync { get; set; }
    }
}
