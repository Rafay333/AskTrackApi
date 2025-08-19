using AskTrackApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AskTrackApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly GPSContext _gpsContext;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(GPSContext gpsContext, ILogger<InventoryController> logger)
        {
            _gpsContext = gpsContext;
            _logger = logger;
        }

        // ---------------------------
        // GET INVENTORY (JWT Required) - Return all statuses for the branch
        // ---------------------------
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetInventoryAllStatuses()
        {
            try
            {
                var branch = User.FindFirst("branch")?.Value;
                if (string.IsNullOrEmpty(branch))
                {
                    _logger.LogWarning("Branch not found in JWT token");
                    return Unauthorized("Branch not found in token.");
                }

                _logger.LogInformation($"Fetching inventory for branch: {branch}");

                var devices = await _gpsContext.UserInfos
                    .Where(d => d.GroupAccount == branch)
                    .OrderByDescending(d => d.DeviceId)
                    .Select(d => new
                    {
                        deviceId = d.DeviceId ?? "",
                        groupAccount = d.GroupAccount ?? "",
                        phoneNumber = d.PhoneNumber ?? "",
                        isinstalled = d.isinstalled   // null = pending, false = processing, true = rejected
                    })
                    .ToListAsync();

                _logger.LogInformation($"Found {devices.Count} devices for branch {branch}");

                // Log device status distribution for debugging
                var pending = devices.Count(d => d.isinstalled == null);
                var processing = devices.Count(d => d.isinstalled == false);
                var rejected = devices.Count(d => d.isinstalled == true);

                _logger.LogInformation($"Status distribution - Pending: {pending}, Processing: {processing}, Rejected: {rejected}");

                return Ok(new
                {
                    branch = branch,
                    deviceCount = devices.Count,
                    devices = devices,
                    statusSummary = new
                    {
                        pending = pending,
                        processing = processing,
                        rejected = rejected
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // ---------------------------
        // TEST WITHOUT JWT - all statuses
        // ---------------------------
        [HttpGet("branch/{branchName}")]
        public async Task<IActionResult> GetInventoryByBranch(string branchName)
        {
            try
            {
                if (string.IsNullOrEmpty(branchName))
                {
                    return BadRequest("Branch name is required");
                }

                _logger.LogInformation($"Fetching inventory for branch (no auth): {branchName}");

                var devices = await _gpsContext.UserInfos
                    .Where(d => d.GroupAccount == branchName)
                    .OrderByDescending(d => d.DeviceId)
                    .Select(d => new
                    {
                        deviceId = d.DeviceId ?? "",
                        groupAccount = d.GroupAccount ?? "",
                        phoneNumber = d.PhoneNumber ?? "",
                        isinstalled = d.isinstalled
                    })
                    .ToListAsync();

                return Ok(new
                {
                    branch = branchName,
                    deviceCount = devices.Count,
                    devices = devices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching inventory for branch {branchName}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // ---------------------------
        // ACKNOWLEDGE DEVICE (sets isinstalled = false -> Processing)
        // POST api/inventory/acknowledge/{deviceId}
        // ---------------------------
        [HttpPost("acknowledge/{deviceId}")]
        [Authorize]
        public async Task<IActionResult> AcknowledgeDevice(string deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId))
                {
                    return BadRequest("Device ID is required");
                }

                var branch = User.FindFirst("branch")?.Value;
                if (string.IsNullOrEmpty(branch))
                {
                    _logger.LogWarning("Branch not found in JWT token for acknowledge request");
                    return Unauthorized("Branch not found in token.");
                }

                _logger.LogInformation($"Acknowledging device {deviceId} for branch {branch}");

                var device = await _gpsContext.UserInfos
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.GroupAccount == branch);

                if (device == null)
                {
                    _logger.LogWarning($"Device {deviceId} not found for branch {branch}");
                    return NotFound(new { message = "Device not found for your branch." });
                }

                // Check if device is already acknowledged/processed
                if (device.isinstalled != null)
                {
                    var currentStatus = device.isinstalled == false ? "Processing" : "Rejected";
                    _logger.LogWarning($"Device {deviceId} is already in {currentStatus} status");
                    return BadRequest(new { message = $"Device is already {currentStatus}." });
                }

                // Set device to Processing status
                var oldStatus = device.isinstalled;
                device.isinstalled = false; // mark as Processing

                await _gpsContext.SaveChangesAsync();

                _logger.LogInformation($"Device {deviceId} acknowledged successfully. Status changed from {oldStatus} to Processing");

                return Ok(new
                {
                    message = "Device acknowledged and set to Processing.",
                    deviceId = deviceId,
                    newStatus = "Processing"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error acknowledging device {deviceId}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // ---------------------------
        // REJECT DEVICE (sets isinstalled = true)
        // POST api/inventory/reject/{deviceId}
        // ---------------------------
        [HttpPost("reject/{deviceId}")]
        [Authorize]
        public async Task<IActionResult> RejectDevice(string deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId))
                {
                    return BadRequest("Device ID is required");
                }

                var branch = User.FindFirst("branch")?.Value;
                if (string.IsNullOrEmpty(branch))
                {
                    _logger.LogWarning("Branch not found in JWT token for reject request");
                    return Unauthorized("Branch not found in token.");
                }

                _logger.LogInformation($"Rejecting device {deviceId} for branch {branch}");

                var device = await _gpsContext.UserInfos
                    .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.GroupAccount == branch);

                if (device == null)
                {
                    _logger.LogWarning($"Device {deviceId} not found for branch {branch}");
                    return NotFound(new { message = "Device not found for your branch." });
                }

                // Check if device is already rejected
                if (device.isinstalled == true)
                {
                    _logger.LogWarning($"Device {deviceId} is already rejected");
                    return BadRequest(new { message = "Device is already rejected." });
                }

                // Set device to Rejected status
                var oldStatus = device.isinstalled;
                device.isinstalled = true; // mark as Rejected

                await _gpsContext.SaveChangesAsync();

                _logger.LogInformation($"Device {deviceId} rejected successfully. Status changed from {oldStatus} to Rejected");

                return Ok(new
                {
                    message = "Device rejected successfully.",
                    deviceId = deviceId,
                    newStatus = "Rejected"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rejecting device {deviceId}");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        
    }
}