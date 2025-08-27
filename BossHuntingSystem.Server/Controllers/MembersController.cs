using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BossHuntingSystem.Server.Data;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly BossHuntingDbContext _context;

        public MembersController(BossHuntingDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetAll()
        {
            try
            {
                var members = await _context.Members
                    .OrderByDescending(m => m.CombatPower)
                    .ThenBy(m => m.Name)
                                         .Select(m => new MemberDto
                     {
                         Id = m.Id,
                         Name = m.Name,
                         CombatPower = m.CombatPower,
                         GcashNumber = m.GcashNumber,
                         GcashName = m.GcashName,
                         CreatedAtUtc = m.CreatedAtUtc,
                         UpdatedAtUtc = m.UpdatedAtUtc
                     })
                    .ToListAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(members);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetAll] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<MemberDto>> GetById(int id)
        {
            try
            {
                var member = await _context.Members.FindAsync(id);
                if (member == null) return NotFound();
                
                var dto = new MemberDto
                {
                    Id = member.Id,
                    Name = member.Name,
                    CombatPower = member.CombatPower,
                    GcashNumber = member.GcashNumber,
                    GcashName = member.GcashName,
                    CreatedAtUtc = member.CreatedAtUtc,
                    UpdatedAtUtc = member.UpdatedAtUtc
                };
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetById] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost]
        public async Task<ActionResult<MemberDto>> Create([FromBody] CreateUpdateMemberDto dto)
        {
            if (dto == null) return BadRequest("Request body is required");
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
            if (dto.CombatPower < 0) return BadRequest("Combat power must be non-negative");

            try
            {
                // Check if member with same name already exists
                var existingMember = await _context.Members.FirstOrDefaultAsync(m => m.Name.ToLower() == dto.Name.ToLower());
                if (existingMember != null)
                {
                    return BadRequest($"Member with name '{dto.Name}' already exists");
                }

                var member = new Member
                {
                    Name = dto.Name.Trim(),
                    CombatPower = dto.CombatPower,
                    GcashNumber = dto.GcashNumber?.Trim(),
                    GcashName = dto.GcashName?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                _context.Members.Add(member);
                await _context.SaveChangesAsync();

                var responseDto = new MemberDto
                {
                    Id = member.Id,
                    Name = member.Name,
                    CombatPower = member.CombatPower,
                    GcashNumber = member.GcashNumber,
                    GcashName = member.GcashName,
                    CreatedAtUtc = member.CreatedAtUtc,
                    UpdatedAtUtc = member.UpdatedAtUtc
                };
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return CreatedAtAction(nameof(GetById), new { id = member.Id }, responseDto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Create] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<MemberDto>> Update(int id, [FromBody] CreateUpdateMemberDto dto)
        {
            if (dto == null) return BadRequest("Request body is required");
            if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Name is required");
            if (dto.CombatPower < 0) return BadRequest("Combat power must be non-negative");

            try
            {
                var existingMember = await _context.Members.FindAsync(id);
                if (existingMember == null) return NotFound();

                // Check if another member with the same name exists (excluding current member)
                var duplicateMember = await _context.Members.FirstOrDefaultAsync(m => 
                    m.Name.ToLower() == dto.Name.ToLower() && m.Id != id);
                if (duplicateMember != null)
                {
                    return BadRequest($"Member with name '{dto.Name}' already exists");
                }

                existingMember.Name = dto.Name.Trim();
                existingMember.CombatPower = dto.CombatPower;
                existingMember.GcashNumber = dto.GcashNumber?.Trim();
                existingMember.GcashName = dto.GcashName?.Trim();
                existingMember.UpdatedAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var responseDto = new MemberDto
                {
                    Id = existingMember.Id,
                    Name = existingMember.Name,
                    CombatPower = existingMember.CombatPower,
                    GcashNumber = existingMember.GcashNumber,
                    GcashName = existingMember.GcashName,
                    CreatedAtUtc = existingMember.CreatedAtUtc,
                    UpdatedAtUtc = existingMember.UpdatedAtUtc
                };
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(responseDto);
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
            try
            {
                var member = await _context.Members.FindAsync(id);
                if (member == null) return NotFound();

                _context.Members.Remove(member);
                await _context.SaveChangesAsync();
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Delete] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost("sync-from-attendance")]
        public async Task<ActionResult<SyncResultDto>> SyncFromAttendance()
        {
            try
            {
                // Get all BossDefeat records first, then process attendees in memory
                var allRecords = await _context.BossDefeats.ToListAsync();
                
                // Extract all unique attendee names from the records
                var allAttendees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var record in allRecords)
                {
                    var attendees = record.Attendees;
                    foreach (var attendee in attendees)
                    {
                        if (!string.IsNullOrWhiteSpace(attendee))
                        {
                            allAttendees.Add(attendee.Trim());
                        }
                    }
                }

                // Get existing member names (case-insensitive comparison)
                var existingMembers = await _context.Members
                    .Select(m => m.Name.ToLower())
                    .ToListAsync();

                var newMembers = new List<Member>();
                var addedCount = 0;

                foreach (var attendeeName in allAttendees)
                {
                    // Check if member with this name already exists (case-insensitive)
                    if (!existingMembers.Contains(attendeeName.ToLower()))
                    {
                        var newMember = new Member
                        {
                            Name = attendeeName,
                            CombatPower = 0, // Default combat power
                            GcashNumber = null,
                            GcashName = null,
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow
                        };
                        newMembers.Add(newMember);
                        addedCount++;
                    }
                }

                if (newMembers.Any())
                {
                    _context.Members.AddRange(newMembers);
                    await _context.SaveChangesAsync();
                }

                var result = new SyncResultDto
                {
                    TotalAttendees = allAttendees.Count,
                    NewMembersAdded = addedCount,
                    TotalMembers = await _context.Members.CountAsync()
                };
                
                // Add cache control headers to prevent caching
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SyncFromAttendance] Error: {ex.Message}");
                return StatusCode(500, "Database error occurred");
            }
        }
    }

    // DTOs
    public class MemberDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CombatPower { get; set; }
        public string? GcashNumber { get; set; }
        public string? GcashName { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }

    public class CreateUpdateMemberDto
    {
        public string Name { get; set; } = string.Empty;
        public int CombatPower { get; set; }
        public string? GcashNumber { get; set; }
        public string? GcashName { get; set; }
    }

    public class SyncResultDto
    {
        public int TotalAttendees { get; set; }
        public int NewMembersAdded { get; set; }
        public int TotalMembers { get; set; }
    }
}
