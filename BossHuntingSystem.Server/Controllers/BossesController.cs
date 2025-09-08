using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Services;
using BossHuntingSystem.Server.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                Owner = boss.Owner
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
                    LastKilledAt = lastKilledAtUtc,
                    Owner = dto.Owner?.Trim()
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
                existing.LastKilledAt = lastKilledAtUtc; // Store as UTC, no conversion needed
                existing.Owner = dto.Owner?.Trim();

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
            _logger.LogInformation("Attempting to delete boss {Id}", id);

            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent boss {Id}", id);
                    return NotFound();
                }

                _context.Bosses.Remove(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted boss {Id} with name {Name}", id, existing.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete boss {Id}", id);
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("{id:int}/defeat")]
        public async Task<ActionResult<BossResponseDto>> Defeat(int id, [FromBody] DefeatBossDto? dto = null)
        {
            _logger.LogInformation("Attempting to record defeat for boss {Id}", id);

            try
            {
                var existing = await _context.Bosses.FindAsync(id);
                if (existing == null) 
                {
                    _logger.LogWarning("Attempted to record defeat for non-existent boss {Id}", id);
                    return NotFound();
                }

                // When a boss is defeated, we set the last kill time to now (UTC)
                // Use the same timezone handling as other methods for consistency
                var currentUtc = DateTime.UtcNow;
                existing.LastKilledAt = currentUtc;
                existing.Owner = dto?.Owner?.Trim();

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

                _logger.LogInformation("Successfully recorded defeat for boss {Id} with name {Name}", id, existing.Name);

                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(ToBossResponseDto(existing));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record defeat for boss {Id}", id);
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
                    Owner = dto?.Owner?.Trim(),
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
                
                // Add to new attendee details system (default: not late, 1.0 points)
                var attendeeDetails = record.AttendeeDetails;
                
                // Check if attendee already exists
                if (attendeeDetails.Any(a => a.Name.Equals(attendeeName, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("Attendee already exists");
                }
                
                attendeeDetails.Add(new Data.AttendeeInfo
                {
                    Name = attendeeName,
                    IsLate = false,
                    Points = 1.0m
                });
                record.AttendeeDetails = attendeeDetails;

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
        
        [HttpPost("history/{id:int}/attendee-late")]
        public async Task<ActionResult<BossDefeat>> AddLateAttendee(int id, [FromBody] AddTextDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Text is required");

            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null) return NotFound();

                var attendeeName = dto.Text.Trim();
                
                // Add to new attendee details system (late: 0.5 points)
                var attendeeDetails = record.AttendeeDetails;
                
                // Check if attendee already exists
                if (attendeeDetails.Any(a => a.Name.Equals(attendeeName, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("Attendee already exists");
                }
                
                attendeeDetails.Add(new Data.AttendeeInfo
                {
                    Name = attendeeName,
                    IsLate = true,
                    Points = 0.5m
                });
                record.AttendeeDetails = attendeeDetails;

                await _context.SaveChangesAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddLateAttendee] Error: {ex.Message}");
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

                // Work with AttendeeDetails (which contains points info) instead of legacy Attendees
                var attendeeDetails = record.AttendeeDetails;
                if (index < 0 || index >= attendeeDetails.Count) return BadRequest("Index out of range");

                attendeeDetails.RemoveAt(index);
                record.AttendeeDetails = attendeeDetails; // This automatically updates legacy Attendees property

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
        public async Task<IActionResult> DeleteHistory(int id)
        {
            _logger.LogInformation("Attempting to delete boss history record {Id}", id);

            try
            {
                var record = await _context.BossDefeats.FindAsync(id);
                if (record == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent boss history record {Id}", id);
                    return NotFound();
                }

                _context.BossDefeats.Remove(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted boss history record {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete boss history record {Id}", id);
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("notify")]
        public async Task<IActionResult> SendManualNotification([FromBody] ManualNotificationDto dto)
        {
            _logger.LogInformation("Attempting to send manual notification for boss {BossName}", dto?.BossName);

            if (dto == null || string.IsNullOrWhiteSpace(dto.BossName))
            {
                _logger.LogWarning("Attempted to send manual notification with invalid data");
                return BadRequest("Boss name is required");
            }

            try
            {
                await _discordService.SendBossNotificationAsync(dto.BossName, dto.MinutesUntilRespawn ?? 5, dto.Owner);
                
                _logger.LogInformation("Successfully sent manual notification for boss {BossName} owned by {Owner}", dto.BossName, dto.Owner ?? "Unknown");
                
                return Ok(new { message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send manual notification for boss {BossName}", dto.BossName);
                return StatusCode(500, "Failed to send notification");
            }
        }

        [HttpGet("points")]
        public async Task<ActionResult<IEnumerable<MemberPointsDto>>> GetMemberPoints()
        {
            try
            {
                // Get all boss defeats with attendance data
                var defeats = await _context.BossDefeats.ToListAsync();
                
                // Dictionary to track points for each member (now using decimal for late attendance)
                var memberPoints = new Dictionary<string, decimal>();
                var memberBattleCount = new Dictionary<string, int>();
                
                foreach (var defeat in defeats)
                {
                    // Use new AttendeeDetails if available, otherwise fallback to legacy Attendees
                    var attendeeDetails = defeat.AttendeeDetails;
                    
                    foreach (var attendeeInfo in attendeeDetails)
                    {
                        if (!string.IsNullOrWhiteSpace(attendeeInfo.Name))
                        {
                            var attendeeName = attendeeInfo.Name;
                            var points = attendeeInfo.IsLate ? 0.5m : 1.0m;
                            
                            if (memberPoints.ContainsKey(attendeeName))
                            {
                                memberPoints[attendeeName] += points;
                                memberBattleCount[attendeeName]++;
                            }
                            else
                            {
                                memberPoints[attendeeName] = points;
                                memberBattleCount[attendeeName] = 1;
                            }
                        }
                    }
                }
                
                // Convert to DTO and sort by points descending
                var result = memberPoints
                    .Select(kvp => new MemberPointsDto
                    {
                        MemberName = kvp.Key,
                        Points = kvp.Value,
                        BossesAttended = memberBattleCount.ContainsKey(kvp.Key) ? memberBattleCount[kvp.Key] : 0
                    })
                    .OrderByDescending(mp => mp.Points)
                    .ThenBy(mp => mp.MemberName)
                    .ToList();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member points");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("calculate-dividends")]
        public async Task<ActionResult<DividendsCalculationResult>> CalculateDividends([FromBody] DividendsCalculationRequest request)
        {
            if (request == null || request.TotalSales <= 0)
            {
                return BadRequest("Total sales must be greater than 0");
            }

            try
            {
                // Get member points for the specified time range or all time
                var defeats = await GetDefeatsByDateRange(request.StartDate, request.EndDate);
                
                // Calculate member points from defeats (using new decimal system)
                var memberPoints = new Dictionary<string, decimal>();
                
                foreach (var defeat in defeats)
                {
                    // Use new AttendeeDetails if available, otherwise fallback to legacy Attendees
                    var attendeeDetails = defeat.AttendeeDetails;
                    
                    foreach (var attendeeInfo in attendeeDetails)
                    {
                        if (!string.IsNullOrWhiteSpace(attendeeInfo.Name))
                        {
                            var attendeeName = attendeeInfo.Name;
                            var points = attendeeInfo.IsLate ? 0.5m : 1.0m;
                            
                            if (memberPoints.ContainsKey(attendeeName))
                            {
                                memberPoints[attendeeName] += points;
                            }
                            else
                            {
                                memberPoints[attendeeName] = points;
                            }
                        }
                    }
                }

                var totalPoints = memberPoints.Values.Sum();
                
                if (totalPoints == 0)
                {
                    return BadRequest("No members with points found for the specified period");
                }

                // Calculate dividends using the formula: Dividend = (Total Sales / Total Points) × Player's Points
                var dividends = memberPoints
                    .Select(kvp => new MemberDividendDto
                    {
                        MemberName = kvp.Key,
                        Points = kvp.Value,
                        Dividend = Math.Round((request.TotalSales / totalPoints) * kvp.Value, 2)
                    })
                    .OrderByDescending(d => d.Dividend)
                    .ThenBy(d => d.MemberName)
                    .ToList();

                var result = new DividendsCalculationResult
                {
                    TotalSales = request.TotalSales,
                    TotalPoints = totalPoints,
                    PeriodStart = request.StartDate,
                    PeriodEnd = request.EndDate,
                    MemberDividends = dividends,
                    CalculatedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating dividends");
                return StatusCode(500, "Internal server error");
            }
        }

        private async Task<List<BossDefeat>> GetDefeatsByDateRange(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.BossDefeats.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(d => d.DefeatedAtUtc >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(d => d.DefeatedAtUtc <= endDate.Value);
            }

            return await query.ToListAsync();
        }


    }

    // DTOs - keeping these in the same file for now but they could be moved to separate files
    public class BossCreateUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public int RespawnHours { get; set; }
        public string? LastKilledAt { get; set; }
        public string? Owner { get; set; }
    }

    public class ManualNotificationDto
    {
        public string BossName { get; set; } = string.Empty;
        public int? MinutesUntilRespawn { get; set; }
        public string? Owner { get; set; }
    }

    public class BossResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RespawnHours { get; set; }
        public DateTime LastKilledAt { get; set; } // This will be in PHT (Philippine Time)
        public DateTime NextRespawnAt { get; set; } // This will be in PHT (Philippine Time)
        public bool IsAvailable { get; set; }
        public string? Owner { get; set; }
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
        public string? Owner { get; set; }
    }
    
    public class AddHistoryDto
    {
        public string? Owner { get; set; }
        public string? DefeatedAt { get; set; } // Optional custom defeated time in PHT
    }
    
    public class MemberPointsDto
    {
        public string MemberName { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public int BossesAttended { get; set; }
    }

    public class DividendsCalculationRequest
    {
        public decimal TotalSales { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class MemberDividendDto
    {
        public string MemberName { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public decimal Dividend { get; set; }
    }

    public class DividendsCalculationResult
    {
        public decimal TotalSales { get; set; }
        public decimal TotalPoints { get; set; }
        public DateTime? PeriodStart { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public List<MemberDividendDto> MemberDividends { get; set; } = new();
        public DateTime CalculatedAt { get; set; }
    }
}