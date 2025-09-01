using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Services;
using BossHuntingSystem.Server.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using BossHuntingSystem.Server.Extensions;
using Microsoft.Extensions.Logging;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "User")] // Require authentication for all endpoints
    public class BossesController : ControllerBase
    {
        private readonly BossHuntingDbContext _context;
        private readonly IDiscordNotificationService _discordService;
        private readonly ILogger<BossesController> _logger;

        public BossesController(BossHuntingDbContext context, IDiscordNotificationService discordService, ILogger<BossesController> logger)
        {
            _context = context;
            _discordService = discordService;
            _logger = logger;
        }

        // Philippine Time Zone
        private static readonly TimeZoneInfo PhilippineTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

        // Helper methods for timezone conversion
        private static DateTime ConvertPhtToUtc(DateTime phtDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(phtDateTime, PhilippineTimeZone);
        }

        private static DateTime ConvertUtcToPht(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, PhilippineTimeZone);
        }

        private static BossResponseDto ToBossResponseDto(Boss boss)
        {
            // Ensure the database value is treated as UTC
            var lastKilledAtUtc = boss.LastKilledAt.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(boss.LastKilledAt, DateTimeKind.Utc)
                : boss.LastKilledAt.ToUniversalTime();
            
            var nextRespawnAtUtc = lastKilledAtUtc.AddHours(boss.RespawnHours);
            var currentUtc = DateTime.UtcNow;

            // Convert UTC times to PHT before sending to frontend
            var lastKilledAtPht = ConvertUtcToPht(lastKilledAtUtc);
            var nextRespawnAtPht = ConvertUtcToPht(nextRespawnAtUtc);
            var currentPht = ConvertUtcToPht(currentUtc);

            return new BossResponseDto
            {
                Id = boss.Id,
                Name = boss.Name,
                RespawnHours = boss.RespawnHours,
                LastKilledAt = lastKilledAtPht,
                NextRespawnAt = nextRespawnAtPht,
                IsAvailable = nextRespawnAtPht <= currentPht,
                Killer = boss.Killer
            };
        }

        // Static method for background service access (will need to be updated separately)
        public static async Task<List<Boss>> GetBossesForNotificationAsync(BossHuntingDbContext context)
        {
            return await context.Bosses.ToListAsync();
        }

        [HttpGet]
        [Authorize(Policy = "ReadOnly")] // Require authentication to view bosses
        public async Task<ActionResult<IEnumerable<BossResponseDto>>> GetAll()
        {
            try
            {
                var bosses = await _context.Bosses.ToListAsync();
                var response = bosses.Select(ToBossResponseDto).ToList();
                
                // Sort by nearest respawn time (available bosses first, then by next respawn time)
                response = response.OrderBy(b => b.IsAvailable ? 0 : 1)
                                 .ThenBy(b => b.NextRespawnAt)
                                 .ToList();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetAll] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpGet("history")]
        [Authorize(Policy = "ReadOnly")] // Require authentication to view history
        public async Task<ActionResult<IEnumerable<BossDefeat>>> GetHistory()
        {
            try
            {
                var history = await _context.BossDefeats
                    .OrderByDescending(h => h.DefeatedAtUtc)
                    .Take(200)
                    .ToListAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(history);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetHistory] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpGet("history/{id:int}")]
        [Authorize(Policy = "ReadOnly")] // Require authentication for individual history records
        public async Task<ActionResult<BossDefeat>> GetHistoryById(int id)
        {
            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null) return NotFound();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetHistoryById] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = "ReadOnly")] // Require authentication for individual boss details
        public async Task<ActionResult<BossResponseDto>> GetById(int id)
        {
            try
            {
                var boss = await _context.Bosses.FindAsync(id);
                if (boss == null) return NotFound();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(ToBossResponseDto(boss));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetById] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost]
        [Authorize(Policy = "BossManagement")] // Only admins can create bosses
        public async Task<ActionResult<BossResponseDto>> Create([FromBody] BossCreateUpdateDto dto)
        {
            Console.WriteLine($"[Create] Received request: Name='{dto?.Name}', RespawnHours={dto?.RespawnHours}, LastKilledAt='{dto?.LastKilledAt}'");

            if (dto == null)
            {
                Console.WriteLine("[Create] Request body is null");
                return BadRequest("Request body is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                Console.WriteLine("[Create] Name is null or empty");
                return BadRequest("Name is required");
            }

            if (dto.RespawnHours <= 0)
            {
                Console.WriteLine($"[Create] Invalid RespawnHours: {dto.RespawnHours}");
                return BadRequest("RespawnHours must be greater than 0");
            }

            try
            {
                DateTime lastKilledAtUtc;

                if (string.IsNullOrEmpty(dto.LastKilledAt))
                {
                    lastKilledAtUtc = DateTime.UtcNow;
                }
                else
                {
                    if (DateTime.TryParse(dto.LastKilledAt, out DateTime parsedDateTime))
                    {
                        // Frontend sends UTC ISO string, so parse it directly as UTC
                        lastKilledAtUtc = parsedDateTime.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc)
                            : parsedDateTime.ToUniversalTime();
                    }
                    else
                    {
                        return BadRequest("Invalid LastKilledAt format");
                    }
                }

                var boss = new Boss
                {
                    Name = dto.Name.Trim(),
                    RespawnHours = dto.RespawnHours,
                    LastKilledAt = lastKilledAtUtc,
                    Killer = dto.Killer?.Trim()
                };

                _context.Bosses.Add(boss);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[Create] Boss created successfully with ID: {boss.Id}, RespawnHours: {boss.RespawnHours}");
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return CreatedAtAction(nameof(GetById), new { id = boss.Id }, ToBossResponseDto(boss));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Create] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "BossManagement")] // Only admins can update bosses
        public async Task<ActionResult<BossResponseDto>> Update(int id, [FromBody] BossCreateUpdateDto dto)
        {
            if (dto == null) return BadRequest("Request body is required");
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
            if (dto.RespawnHours <= 0) return BadRequest("RespawnHours must be greater than 0");

            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null) return NotFound();

                DateTime lastKilledAtUtc;

                if (string.IsNullOrEmpty(dto.LastKilledAt))
                {
                    lastKilledAtUtc = DateTime.UtcNow;
                }
                else
                {
                    if (DateTime.TryParse(dto.LastKilledAt, out DateTime parsedDateTime))
                    {
                        // Frontend sends UTC ISO string, so parse it directly as UTC
                        lastKilledAtUtc = parsedDateTime.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc)
                            : parsedDateTime.ToUniversalTime();
                    }
                    else
                    {
                        return BadRequest("Invalid LastKilledAt format");
                    }
                }

                existing.Name = dto.Name.Trim();
                existing.RespawnHours = dto.RespawnHours;
                existing.LastKilledAt = lastKilledAtUtc.AddHours(-8);
                existing.Killer = dto.Killer?.Trim();

                await _context.SaveChangesAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(ToBossResponseDto(existing));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Update] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "BossManagement")] // Only admins can delete bosses
        public async Task<IActionResult> Delete(int id)
        {
            var username = User.GetUsername();
            _logger.LogInformation("User {Username} attempting to delete boss {Id}", username, id);

            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("User {Username} attempted to delete non-existent boss {Id}", username, id);
                    return NotFound();
                }

                _context.Bosses.Remove(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} successfully deleted boss {Id} with name {Name}", username, id, existing.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} failed to delete boss {Id}", username, id);
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("{id:int}/defeat")]
        [Authorize(Policy = "BossManagement")] // Only admins can record boss defeats
        public async Task<ActionResult<BossResponseDto>> Defeat(int id, [FromBody] DefeatBossDto? dto = null)
        {
            var username = User.GetUsername();
            _logger.LogInformation("User {Username} attempting to record defeat for boss {Id}", username, id);

            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null) 
                {
                    _logger.LogWarning("User {Username} attempted to record defeat for non-existent boss {Id}", username, id);
                    return NotFound();
                }

                // When a boss is defeated, we set the last kill time to now (UTC)
                // Use the same timezone handling as other methods for consistency
                var currentUtc = DateTime.UtcNow;
                existing.LastKilledAt = currentUtc;
                existing.Killer = dto?.Killer?.Trim();

                var defeat = new BossDefeat
                {
                    BossId = existing.Id,
                    BossName = existing.Name,
                    DefeatedAtUtc = currentUtc, // Store as UTC for consistency
                    LootsJson = "[]",
                    AttendeesJson = "[]"
                };

                _context.BossDefeats.Add(defeat);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} successfully recorded defeat for boss {Id} with name {Name}", username, id, existing.Name);

                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(ToBossResponseDto(existing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} failed to record defeat for boss {Id}", username, id);
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("{id:int}/add-history")]
        public async Task<ActionResult<BossDefeat>> AddHistory(int id, [FromBody] AddHistoryDto? dto = null)
        {
            Console.WriteLine($"[AddHistory] Request received for boss ID: {id}");
            
            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null) return NotFound();

                Console.WriteLine($"[AddHistory] Creating history record for boss: {existing.Name} (ID: {existing.Id})");

                // Create history record with custom time or current datetime
                DateTime defeatedAtUtc;
                
                if (!string.IsNullOrEmpty(dto?.DefeatedAt))
                {
                    // Parse the custom PHT time and convert to UTC
                    if (DateTime.TryParse(dto.DefeatedAt, out DateTime customPhtTime))
                    {
                        defeatedAtUtc = ConvertPhtToUtc(customPhtTime);
                    }
                    else
                    {
                        return BadRequest("Invalid DefeatedAt format");
                    }
                }
                else
                {
                    // Use current UTC time if no custom time provided
                    defeatedAtUtc = DateTime.UtcNow;
                }
                
                var historyRecord = new BossDefeat
                {
                    BossId = existing.Id,
                    BossName = existing.Name,
                    DefeatedAtUtc = defeatedAtUtc,
                    Killer = dto?.Killer?.Trim(),
                    LootsJson = "[]",
                    AttendeesJson = "[]"
                };

                _context.BossDefeats.Add(historyRecord);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[AddHistory] History record created successfully with ID: {historyRecord.Id} at {defeatedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");

                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(historyRecord);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddHistory] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("history/{id:int}/loot")]
        public async Task<ActionResult<BossDefeat>> AddLoot(int id, [FromBody] AddTextDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Text is required");

            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null) return NotFound();

                // Add to both old format (for backward compatibility) and new format
                var loots = record.Loots;
                loots.Add(dto.Text.Trim());
                record.Loots = loots;
                
                // Add to new format with price
                var lootItems = record.LootItems;
                lootItems.Add(new Data.LootItem { Name = dto.Text.Trim(), Price = null });
                record.LootItems = lootItems;

                await _context.SaveChangesAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddLoot] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("history/{id:int}/attendee")]
        public async Task<ActionResult<BossDefeat>> AddAttendee(int id, [FromBody] AddTextDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Text is required");

            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null) return NotFound();

                var attendeeName = dto.Text.Trim();
                var attendees = record.Attendees;
                attendees.Add(attendeeName);
                record.Attendees = attendees;

                await _context.SaveChangesAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddAttendee] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpDelete("history/{id:int}/loot/{index:int}")]
        public async Task<ActionResult<BossDefeat>> RemoveLoot(int id, int index)
        {
            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null) return NotFound();

                var loots = record.Loots;
                if (index < 0 || index >= loots.Count) return BadRequest("Index out of range");

                loots.RemoveAt(index);
                record.Loots = loots;
                
                // Also remove from new format
                var lootItems = record.LootItems;
                if (index < lootItems.Count)
                {
                    lootItems.RemoveAt(index);
                    record.LootItems = lootItems;
                }

                await _context.SaveChangesAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoveLoot] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPut("history/{id:int}/loot/{index:int}/price")]
        public async Task<ActionResult<BossDefeat>> UpdateLootPrice(int id, int index, [FromBody] UpdateLootPriceDto dto)
        {
            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null) return NotFound();

                var lootItems = record.LootItems;
                if (index < 0 || index >= lootItems.Count) return BadRequest("Index out of range");

                lootItems[index].Price = dto.Price;
                record.LootItems = lootItems;

                await _context.SaveChangesAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateLootPrice] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpDelete("history/{id:int}/attendee/{index:int}")]
        public async Task<ActionResult<BossDefeat>> RemoveAttendee(int id, int index)
        {
            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null) return NotFound();

                var attendees = record.Attendees;
                if (index < 0 || index >= attendees.Count) return BadRequest("Index out of range");

                attendees.RemoveAt(index);
                record.Attendees = attendees;

                await _context.SaveChangesAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RemoveAttendee] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpDelete("history/{id:int}")]
        [Authorize(Policy = "BossManagement")] // Only admins can delete history records
        public async Task<IActionResult> DeleteHistory(int id)
        {
            var username = User.GetUsername();
            _logger.LogInformation("User {Username} attempting to delete boss history record {Id}", username, id);

            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null)
                {
                    _logger.LogWarning("User {Username} attempted to delete non-existent boss history record {Id}", username, id);
                    return NotFound();
                }

                _context.BossDefeats.Remove(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} successfully deleted boss history record {Id}", username, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} failed to delete boss history record {Id}", username, id);
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("notify")]
        [Authorize(Policy = "Notifications")] // Only admins can send manual notifications
        public async Task<IActionResult> SendManualNotification([FromBody] ManualNotificationDto dto)
        {
            var username = User.GetUsername();
            _logger.LogInformation("User {Username} attempting to send manual notification for boss {BossName}", username, dto?.BossName);

            if (dto == null || string.IsNullOrWhiteSpace(dto.BossName))
            {
                _logger.LogWarning("User {Username} attempted to send manual notification with invalid data", username);
                return BadRequest("Boss name is required");
            }

            try
            {
                await _discordService.SendBossNotificationAsync(dto.BossName, dto.MinutesUntilRespawn ?? 5);
                
                _logger.LogInformation("User {Username} successfully sent manual notification for boss {BossName}", username, dto.BossName);
                
                return Ok(new { message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} failed to send manual notification for boss {BossName}", username, dto.BossName);
                return StatusCode(500, "Failed to send notification");
            }
        }


    }

    // DTOs - keeping these in the same file for now but they could be moved to separate files
    public class BossCreateUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public int RespawnHours { get; set; }
        public string? LastKilledAt { get; set; }
        public string? Killer { get; set; }
    }

    public class ManualNotificationDto
    {
        public string BossName { get; set; } = string.Empty;
        public int? MinutesUntilRespawn { get; set; }
    }

    public class BossResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RespawnHours { get; set; }
        public DateTime LastKilledAt { get; set; } // This will be in PHT (Philippine Time)
        public DateTime NextRespawnAt { get; set; } // This will be in PHT (Philippine Time)
        public bool IsAvailable { get; set; }
        public string? Killer { get; set; }
    }

    public class AddTextDto
    {
        public string Text { get; set; } = string.Empty;
    }
    
    public class LootItemDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
    }
    
    public class UpdateLootPriceDto
    {
        public int Index { get; set; }
        public decimal? Price { get; set; }
    }
    
    public class DefeatBossDto
    {
        public string? Killer { get; set; }
    }
    
    public class AddHistoryDto
    {
        public string? Killer { get; set; }
        public string? DefeatedAt { get; set; } // Optional custom defeated time in PHT
    }
}