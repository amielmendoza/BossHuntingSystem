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

        // Static method to allow background service access to boss data
        public static List<Boss> GetBossesForNotification()
        {
            lock (Sync)
            {
                return new List<Boss>(Bosses);
            }
        }

        [HttpGet]
        public ActionResult<IEnumerable<Boss>> GetAll()
        {
            lock (Sync)
            {
                return Ok(Bosses.OrderBy(b => b.Id));
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
        public ActionResult<Boss> GetById(int id)
        {
            lock (Sync)
            {
                var boss = Bosses.FirstOrDefault(b => b.Id == id);
                if (boss == null) return NotFound();
                return Ok(boss);
            }
        }

        [HttpPost]
        public ActionResult<Boss> Create([FromBody] BossCreateUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
            if (dto.RespawnMinutes <= 0) return BadRequest("RespawnMinutes must be positive");
            if (string.IsNullOrWhiteSpace(dto.Location)) return BadRequest("Location is required");

            lock (Sync)
            {
                var boss = new Boss
                {
                    Id = _nextId++,
                    Name = dto.Name.Trim(),
                    Location = dto.Location.Trim(),
                    RespawnMinutes = dto.RespawnMinutes,
                    LastKilledAt = dto.LastKilledAt == default ? DateTime.UtcNow : DateTime.SpecifyKind(dto.LastKilledAt, DateTimeKind.Utc)
                };
                Bosses.Add(boss);
                return CreatedAtAction(nameof(GetById), new { id = boss.Id }, boss);
            }
        }

        [HttpPut("{id:int}")]
        public ActionResult<Boss> Update(int id, [FromBody] BossCreateUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
            if (dto.RespawnMinutes <= 0) return BadRequest("RespawnMinutes must be positive");
            if (string.IsNullOrWhiteSpace(dto.Location)) return BadRequest("Location is required");

            lock (Sync)
            {
                var existing = Bosses.FirstOrDefault(b => b.Id == id);
                if (existing == null) return NotFound();

                existing.Name = dto.Name.Trim();
                existing.Location = dto.Location.Trim();
                existing.RespawnMinutes = dto.RespawnMinutes;
                existing.LastKilledAt = dto.LastKilledAt == default ? existing.LastKilledAt : DateTime.SpecifyKind(dto.LastKilledAt, DateTimeKind.Utc);
                return Ok(existing);
            }
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            lock (Sync)
            {
                var existing = Bosses.FirstOrDefault(b => b.Id == id);
                if (existing == null) return NotFound();
                Bosses.Remove(existing);
                return NoContent();
            }
        }

        [HttpPost("{id:int}/defeat")]
        public ActionResult<Boss> Defeat(int id)
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
                    Location = existing.Location,
                    DefeatedAtUtc = existing.LastKilledAt,
                    Loots = new List<string>(),
                    Attendees = new List<string>()
                });
                return Ok(existing);
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
        public string Location { get; set; } = string.Empty;
        public int RespawnMinutes { get; set; }
        public DateTime LastKilledAt { get; set; }
    }

    public class BossCreateUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int RespawnMinutes { get; set; }
        public DateTime LastKilledAt { get; set; }
    }

    public class BossDefeat
    {
        public int Id { get; set; }
        public int BossId { get; set; }
        public string BossName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime DefeatedAtUtc { get; set; }
        public List<string> Loots { get; set; } = new();
        public List<string> Attendees { get; set; } = new();
    }

    public class AddTextDto
    {
        public string Text { get; set; } = string.Empty;
    }
}


