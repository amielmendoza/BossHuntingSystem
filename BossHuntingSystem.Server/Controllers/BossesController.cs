using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BossesController : ControllerBase
    {
        private static readonly object Sync = new();
        private static int _nextId = 1;
        private static readonly List<Boss> Bosses = new();

        private static readonly List<BossDefeat> History = new();

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
            var lastKilledAtUtc = DateTime.SpecifyKind(boss.LastKilledAt, DateTimeKind.Utc);
            var nextRespawnAtUtc = lastKilledAtUtc.AddHours(boss.RespawnHours);
            var currentUtc = DateTime.UtcNow;

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

        // Static method to allow background service access to boss data
        public static List<Boss> GetBossesForNotification()
        {
            lock (Sync)
            {
                return new List<Boss>(Bosses);
            }
        }

        [HttpGet]
        public ActionResult<IEnumerable<BossResponseDto>> GetAll()
        {
            lock (Sync)
            {
                var response = Bosses.OrderBy(b => b.Id).Select(ToBossResponseDto).ToList();
                return Ok(response);
            }
        }

        [HttpGet("history")]
        public ActionResult<IEnumerable<BossDefeat>> GetHistory()
        {
            lock (Sync)
            {
                return Ok(History.OrderByDescending(h => h.DefeatedAtUtc).Take(200));
            }
        }

        [HttpGet("history/{id:int}")]
        public ActionResult<BossDefeat> GetHistoryById(int id)
        {
            lock (Sync)
            {
                var rec = History.FirstOrDefault(h => h.Id == id);
                if (rec == null) return NotFound();
                return Ok(rec);
            }
        }

        [HttpGet("{id:int}")]
        public ActionResult<BossResponseDto> GetById(int id)
        {
            lock (Sync)
            {
                var boss = Bosses.FirstOrDefault(b => b.Id == id);
                if (boss == null) return NotFound();
                return Ok(ToBossResponseDto(boss));
            }
        }

        [HttpPost]
        public ActionResult<BossResponseDto> Create([FromBody] BossCreateUpdateDto dto)
        {
            Console.WriteLine($"[Create] Received request: Name='{dto?.Name}', RespawnHours={dto?.RespawnHours}, LastKilledAt='{dto?.LastKilledAt}'");
            
            if (dto == null) return BadRequest("Request body is null");
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
            if (dto.RespawnHours <= 0) return BadRequest("RespawnHours must be positive");

            lock (Sync)
            {
                DateTime lastKilledAtUtc;
                if (string.IsNullOrEmpty(dto.LastKilledAt))
                {
                    // If no time specified, use current UTC time
                    lastKilledAtUtc = DateTime.UtcNow;
                }
                else
                {
                    // Try to parse the input datetime string
                    if (DateTime.TryParse(dto.LastKilledAt, out DateTime parsedDateTime))
                    {
                        // Treat input as PHT and convert to UTC for storage
                        var phtTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Unspecified);
                        lastKilledAtUtc = ConvertPhtToUtc(phtTime);
                    }
                    else
                    {
                        return BadRequest("Invalid LastKilledAt format");
                    }
                }

                var boss = new Boss
                {
                    Id = _nextId++,
                    Name = dto.Name.Trim(),
                    RespawnHours = dto.RespawnHours,
                    LastKilledAt = lastKilledAtUtc
                };
                Bosses.Add(boss);
                return CreatedAtAction(nameof(GetById), new { id = boss.Id }, ToBossResponseDto(boss));
            }
        }

        [HttpPut("{id:int}")]
        public ActionResult<BossResponseDto> Update(int id, [FromBody] BossCreateUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
            if (dto.RespawnHours <= 0) return BadRequest("RespawnHours must be positive");

            lock (Sync)
            {
                var existing = Bosses.FirstOrDefault(b => b.Id == id);
                if (existing == null) return NotFound();

                existing.Name = dto.Name.Trim();
                existing.RespawnHours = dto.RespawnHours;
                
                if (!string.IsNullOrEmpty(dto.LastKilledAt))
                {
                    // Try to parse the input datetime string
                    if (DateTime.TryParse(dto.LastKilledAt, out DateTime parsedDateTime))
                    {
                        // Treat input as PHT and convert to UTC for storage
                        var phtTime = DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Unspecified);
                        existing.LastKilledAt = ConvertPhtToUtc(phtTime);
                    }
                    else
                    {
                        return BadRequest("Invalid LastKilledAt format");
                    }
                }
                return Ok(ToBossResponseDto(existing));
            }
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            Console.WriteLine($"[BossesController] DELETE request received for ID: {id}");
            lock (Sync)
            {
                var existing = Bosses.FirstOrDefault(b => b.Id == id);
                if (existing == null) 
                {
                    Console.WriteLine($"[BossesController] Boss with ID {id} not found");
                    return NotFound();
                }
                
                Bosses.Remove(existing);
                Console.WriteLine($"[BossesController] Boss with ID {id} deleted successfully");
                return NoContent();
            }
        }

        [HttpPost("{id:int}/defeat")]
        public ActionResult<BossResponseDto> Defeat(int id)
        {
            lock (Sync)
            {
                var existing = Bosses.FirstOrDefault(b => b.Id == id);
                if (existing == null) return NotFound();
                // When a boss is defeated, we set the last kill time to now (UTC)
                existing.LastKilledAt = DateTime.UtcNow;
                History.Add(new BossDefeat
                {
                    Id = History.Count == 0 ? 1 : History[^1].Id + 1,
                    BossId = existing.Id,
                    BossName = existing.Name,
                    DefeatedAtUtc = existing.LastKilledAt,
                    Loots = new List<string>(),
                    Attendees = new List<string>()
                });
                return Ok(ToBossResponseDto(existing));
            }
        }

        [HttpPost("{id:int}/add-history")]
        public ActionResult<BossDefeat> AddHistory(int id)
        {
            lock (Sync)
            {
                var existing = Bosses.FirstOrDefault(b => b.Id == id);
                if (existing == null) return NotFound();
                
                // Create history record without resetting the respawn timer
                // DefeatedAtUtc is null since this is just a history entry, not an actual defeat
                var historyRecord = new BossDefeat
                {
                    Id = History.Count == 0 ? 1 : History[^1].Id + 1,
                    BossId = existing.Id,
                    BossName = existing.Name,
                    DefeatedAtUtc = null, // Null because boss wasn't actually defeated
                    Loots = new List<string>(),
                    Attendees = new List<string>()
                };
                
                History.Add(historyRecord);
                return Ok(historyRecord);
            }
        }

        [HttpPost("history/{id:int}/loot")]
        public ActionResult<BossDefeat> AddLoot(int id, [FromBody] AddTextDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Text is required");
            lock (Sync)
            {
                var rec = History.FirstOrDefault(h => h.Id == id);
                if (rec == null) return NotFound();
                rec.Loots.Add(dto.Text.Trim());
                return Ok(rec);
            }
        }

        [HttpPost("history/{id:int}/attendee")]
        public ActionResult<BossDefeat> AddAttendee(int id, [FromBody] AddTextDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text)) return BadRequest("Text is required");
            lock (Sync)
            {
                var rec = History.FirstOrDefault(h => h.Id == id);
                if (rec == null) return NotFound();
                rec.Attendees.Add(dto.Text.Trim());
                return Ok(rec);
            }
        }

        [HttpDelete("history/{id:int}/loot/{index:int}")]
        public ActionResult<BossDefeat> RemoveLoot(int id, int index)
        {
            lock (Sync)
            {
                var rec = History.FirstOrDefault(h => h.Id == id);
                if (rec == null) return NotFound();
                if (index < 0 || index >= rec.Loots.Count) return BadRequest("Index out of range");
                rec.Loots.RemoveAt(index);
                return Ok(rec);
            }
        }

        [HttpDelete("history/{id:int}/attendee/{index:int}")]
        public ActionResult<BossDefeat> RemoveAttendee(int id, int index)
        {
            lock (Sync)
            {
                var rec = History.FirstOrDefault(h => h.Id == id);
                if (rec == null) return NotFound();
                if (index < 0 || index >= rec.Attendees.Count) return BadRequest("Index out of range");
                rec.Attendees.RemoveAt(index);
                return Ok(rec);
            }
        }
    }

    public class Boss
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RespawnHours { get; set; }
        public DateTime LastKilledAt { get; set; }
    }

    public class BossCreateUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public int RespawnHours { get; set; }
        public string? LastKilledAt { get; set; }
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

    public class BossDefeat
    {
        public int Id { get; set; }
        public int BossId { get; set; }
        public string BossName { get; set; } = string.Empty;
        public DateTime? DefeatedAtUtc { get; set; }
        public List<string> Loots { get; set; } = new();
        public List<string> Attendees { get; set; } = new();
    }

    public class AddTextDto
    {
        public string Text { get; set; } = string.Empty;
    }
}


