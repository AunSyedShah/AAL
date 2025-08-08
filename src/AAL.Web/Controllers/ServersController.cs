using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AAL.Web.Data;
using AAL.Web.Models;

namespace AAL.Web.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [ApiController]
    [Route("api/[controller]")]
    public class ServersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ServersController> _logger;

        public ServersController(ApplicationDbContext context, ILogger<ServersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("sync-all")]
        public async Task<IActionResult> SyncAllServers()
        {
            try
            {
                _logger.LogInformation("Starting full system synchronization...");

                // Simulate synchronization with all warehouses
                var warehouses = await _context.Warehouses
                    .Where(w => w.IsActive)
                    .ToListAsync();

                var syncResults = new List<SyncResult>();

                foreach (var warehouse in warehouses)
                {
                    var result = await SyncWarehouseData(warehouse.WarehouseId);
                    syncResults.Add(result);
                }

                // Simulate manufacturing plant sync
                var plantResults = await SyncManufacturingPlants();
                syncResults.AddRange(plantResults);

                var successCount = syncResults.Count(r => r.Success);
                var totalCount = syncResults.Count;

                _logger.LogInformation($"Synchronization completed: {successCount}/{totalCount} successful");

                return Ok(new
                {
                    success = true,
                    message = $"Synchronization completed: {successCount}/{totalCount} servers synchronized successfully",
                    results = syncResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during full system synchronization");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("warehouses/{warehouseId}/sync")]
        public async Task<IActionResult> SyncWarehouse(int warehouseId)
        {
            try
            {
                var warehouse = await _context.Warehouses
                    .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId && w.IsActive);

                if (warehouse == null)
                {
                    return NotFound(new { success = false, message = "Warehouse not found" });
                }

                var result = await SyncWarehouseData(warehouseId);

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    warehouseId = warehouseId,
                    warehouseName = warehouse.Name,
                    syncTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing warehouse {warehouseId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("plants/{plantId}/sync")]
        public async Task<IActionResult> SyncManufacturingPlant(string plantId)
        {
            try
            {
                _logger.LogInformation($"Syncing manufacturing plant {plantId}...");

                // Simulate plant synchronization
                await Task.Delay(Random.Shared.Next(500, 2000)); // Simulate network delay

                var isSuccess = Random.Shared.Next(0, 10) > 1; // 90% success rate

                if (isSuccess)
                {
                    _logger.LogInformation($"Manufacturing plant {plantId} synchronized successfully");
                    return Ok(new
                    {
                        success = true,
                        message = $"Manufacturing plant {plantId} synchronized successfully",
                        plantId = plantId,
                        syncTime = DateTime.UtcNow,
                        dataPoints = Random.Shared.Next(100, 500)
                    });
                }
                else
                {
                    _logger.LogWarning($"Failed to sync manufacturing plant {plantId}");
                    return Ok(new
                    {
                        success = false,
                        message = $"Failed to sync manufacturing plant {plantId} - Connection timeout",
                        plantId = plantId,
                        syncTime = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing manufacturing plant {plantId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("test-connections")]
        public async Task<IActionResult> TestConnections()
        {
            try
            {
                _logger.LogInformation("Testing all server connections...");

                var testResults = new List<ConnectionTestResult>();

                // Test warehouse connections
                var warehouses = await _context.Warehouses
                    .Where(w => w.IsActive)
                    .ToListAsync();

                foreach (var warehouse in warehouses)
                {
                    var result = await TestWarehouseConnection(warehouse.WarehouseId, warehouse.Name);
                    testResults.Add(result);
                }

                // Test manufacturing plant connections
                var plantIds = new[] { "MFG-001", "MFG-002", "MFG-003", "MFG-004" };
                foreach (var plantId in plantIds)
                {
                    var result = await TestPlantConnection(plantId);
                    testResults.Add(result);
                }

                var successful = testResults.Count(r => r.IsOnline);
                var failed = testResults.Count(r => !r.IsOnline);

                return Ok(new
                {
                    success = true,
                    successful = successful,
                    failed = failed,
                    results = testResults,
                    testTime = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connections");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetSystemStatus()
        {
            try
            {
                var warehouses = await _context.Warehouses
                    .Where(w => w.IsActive)
                    .ToListAsync();

                var inventoryItems = await _context.InventoryItems
                    .Include(i => i.Warehouse)
                    .Where(i => i.Warehouse.IsActive)
                    .CountAsync();

                var activeOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing)
                    .CountAsync();

                return Ok(new
                {
                    totalWarehouses = warehouses.Count,
                    totalInventoryItems = inventoryItems,
                    activeOrders = activeOrders,
                    systemUptime = TimeSpan.FromDays(Random.Shared.Next(1, 30)).TotalHours,
                    lastSystemSync = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 30)),
                    serverLoad = Random.Shared.Next(15, 85)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system status");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private async Task<SyncResult> SyncWarehouseData(int warehouseId)
        {
            try
            {
                _logger.LogInformation($"Syncing warehouse {warehouseId}...");

                // Simulate network delay
                await Task.Delay(Random.Shared.Next(500, 2000));

                // Update inventory last updated timestamps
                var inventoryItems = await _context.InventoryItems
                    .Where(i => i.WarehouseId == warehouseId)
                    .ToListAsync();

                foreach (var item in inventoryItems)
                {
                    item.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var isSuccess = Random.Shared.Next(0, 10) > 0; // 95% success rate

                return new SyncResult
                {
                    ServerId = $"WH-{warehouseId}",
                    Success = isSuccess,
                    Message = isSuccess ? "Warehouse synchronized successfully" : "Sync failed - Network timeout",
                    DataPointsSynced = isSuccess ? inventoryItems.Count : 0,
                    SyncTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing warehouse {warehouseId}");
                return new SyncResult
                {
                    ServerId = $"WH-{warehouseId}",
                    Success = false,
                    Message = ex.Message,
                    DataPointsSynced = 0,
                    SyncTime = DateTime.UtcNow
                };
            }
        }

        private async Task<List<SyncResult>> SyncManufacturingPlants()
        {
            var plantIds = new[] { "MFG-001", "MFG-002", "MFG-003", "MFG-004" };
            var results = new List<SyncResult>();

            foreach (var plantId in plantIds)
            {
                try
                {
                    _logger.LogInformation($"Syncing manufacturing plant {plantId}...");
                    await Task.Delay(Random.Shared.Next(300, 1500));

                    var isSuccess = Random.Shared.Next(0, 10) > 1; // 90% success rate

                    results.Add(new SyncResult
                    {
                        ServerId = plantId,
                        Success = isSuccess,
                        Message = isSuccess ? "Plant synchronized successfully" : "Sync failed - Connection error",
                        DataPointsSynced = isSuccess ? Random.Shared.Next(50, 200) : 0,
                        SyncTime = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error syncing plant {plantId}");
                    results.Add(new SyncResult
                    {
                        ServerId = plantId,
                        Success = false,
                        Message = ex.Message,
                        DataPointsSynced = 0,
                        SyncTime = DateTime.UtcNow
                    });
                }
            }

            return results;
        }

        private async Task<ConnectionTestResult> TestWarehouseConnection(int warehouseId, string warehouseName)
        {
            await Task.Delay(Random.Shared.Next(100, 500)); // Simulate ping

            var responseTime = Random.Shared.Next(50, 300);
            var isOnline = Random.Shared.Next(0, 10) > 0; // 95% uptime

            return new ConnectionTestResult
            {
                ServerId = $"WH-{warehouseId}",
                ServerName = warehouseName,
                IsOnline = isOnline,
                ResponseTime = responseTime,
                TestTime = DateTime.UtcNow
            };
        }

        private async Task<ConnectionTestResult> TestPlantConnection(string plantId)
        {
            await Task.Delay(Random.Shared.Next(100, 500)); // Simulate ping

            var responseTime = Random.Shared.Next(80, 400);
            var isOnline = Random.Shared.Next(0, 10) > 1; // 90% uptime

            return new ConnectionTestResult
            {
                ServerId = plantId,
                ServerName = $"Manufacturing Plant {plantId}",
                IsOnline = isOnline,
                ResponseTime = responseTime,
                TestTime = DateTime.UtcNow
            };
        }
    }

    public class SyncResult
    {
        public string ServerId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int DataPointsSynced { get; set; }
        public DateTime SyncTime { get; set; }
    }

    public class ConnectionTestResult
    {
        public string ServerId { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public int ResponseTime { get; set; }
        public DateTime TestTime { get; set; }
    }
}
