using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Services;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BossesController : ControllerBase
    {
        private readonly BossHuntingDbContext _context;
        private readonly IDiscordNotificationService _discordService;

        public BossesController(BossHuntingDbContext context, IDiscordNotificationService discordService)
        {
            _context = context;
            _discordService = discordService;
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

            // Send UTC times to frontend - let frontend handle timezone conversion
            return new BossResponseDto
            {
                Id = boss.Id,
                Name = boss.Name,
                RespawnHours = boss.RespawnHours,
                LastKilledAt = lastKilledAtUtc,
                NextRespawnAt = nextRespawnAtUtc,
                IsAvailable = nextRespawnAtUtc <= currentUtc
            };
        }

        // Static method for background service access (will need to be updated separately)
        public static async Task<List<Boss>> GetBossesForNotificationAsync(BossHuntingDbContext context)
        {
            return await context.Bosses.ToListAsync();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BossResponseDto>>> GetAll()
        {
            try
            {
                var bosses = await _context.Bosses.OrderBy(b => b.Id).ToListAsync();
                var response = bosses.Select(ToBossResponseDto).ToList();
                
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
                    LastKilledAt = lastKilledAtUtc
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
                existing.LastKilledAt = lastKilledAtUtc;

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
        public async Task<IActionResult> Delete(int id)
        {
            Console.WriteLine($"[BossesController] DELETE request received for ID: {id}");
            Console.WriteLine($"[BossesController] Request Headers: {string.Join(", ", Request.Headers.Select(h => $"{h.Key}:{h.Value}"))}");

            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null)
                {
                    Console.WriteLine($"[BossesController] Boss with ID {id} not found");
                    return NotFound();
                }

                _context.Bosses.Remove(existing);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[BossesController] Boss with ID {id} deleted successfully");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Delete] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("{id:int}/defeat")]
        public async Task<ActionResult<BossResponseDto>> Defeat(int id)
        {
            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null) return NotFound();

                // When a boss is defeated, we set the last kill time to now (UTC)
                existing.LastKilledAt = DateTime.UtcNow;

                var defeat = new BossDefeat
                {
                    BossId = existing.Id,
                    BossName = existing.Name,
                    DefeatedAtUtc = existing.LastKilledAt,
                    LootsJson = "[]",
                    AttendeesJson = "[]"
                };

                _context.BossDefeats.Add(defeat);
                await _context.SaveChangesAsync();

                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(ToBossResponseDto(existing));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Defeat] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("{id:int}/add-history")]
        public async Task<ActionResult<BossDefeat>> AddHistory(int id)
        {
            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null) return NotFound();

                // Create history record without resetting the respawn timer
                // DefeatedAtUtc is null since this is just a history entry, not an actual defeat
                var historyRecord = new BossDefeat
                {
                    BossId = existing.Id,
                    BossName = existing.Name,
                    DefeatedAtUtc = null, // Null because boss wasn't actually defeated
                    LootsJson = "[]",
                    AttendeesJson = "[]"
                };

                _context.BossDefeats.Add(historyRecord);
                await _context.SaveChangesAsync();

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

                var loots = record.Loots;
                loots.Add(dto.Text.Trim());
                record.Loots = loots;

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

                var attendees = record.Attendees;
                attendees.Add(dto.Text.Trim());
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

        [HttpPost("notify")]
        public async Task<IActionResult> SendManualNotification([FromBody] ManualNotificationDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Message))
            {
                return BadRequest("Message is required");
            }

            try
            {
                await _discordService.SendManualNotificationAsync(dto.Message);
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(new { success = true, message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendManualNotification] Error: {ex.Message}");
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
    }

    public class ManualNotificationDto
    {
        public string Message { get; set; } = string.Empty;
    }

    public class BossResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RespawnHours { get; set; }
        public DateTime LastKilledAt { get; set; } // This will be in UTC
        public DateTime NextRespawnAt { get; set; } // This will be in UTC
        public bool IsAvailable { get; set; }
    }

    public class AddTextDto
    {
        public string Text { get; set; } = string.Empty;
    }
}