using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Extensions;
using Microsoft.Extensions.Logging;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "User")] // Require authentication for all endpoints
    public class MembersController : ControllerBase
    {
        private readonly BossHuntingDbContext _context;
        private readonly ILogger<MembersController> _logger;

        public MembersController(BossHuntingDbContext context, ILogger<MembersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Policy = "ReadOnly")] // Require authentication to view members
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
                _logger.LogError(ex, "[GetAll] Error retrieving members");
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = "ReadOnly")] // Require authentication to view individual members
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
                _logger.LogError(ex, "[GetById] Error retrieving member {Id}", id);
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPost]
        [Authorize(Policy = "MemberManagement")] // Only admins can create members
        public async Task<ActionResult<MemberDto>> Create([FromBody] CreateUpdateMemberDto dto)
        {
            var username = User.GetUsername();
            _logger.LogInformation("User {Username} attempting to create member: {Name}", username, dto?.Name);

            if (dto == null) 
            {
                _logger.LogWarning("User {Username} attempted to create member with null request body", username);
                return BadRequest("Request body is required");
            }
            
            if (string.IsNullOrWhiteSpace(dto.Name)) 
            {
                _logger.LogWarning("User {Username} attempted to create member with empty name", username);
                return BadRequest("Name is required");
            }
            
            if (dto.CombatPower < 0) 
            {
                _logger.LogWarning("User {Username} attempted to create member with invalid combat power: {CombatPower}", username, dto.CombatPower);
                return BadRequest("Combat power must be non-negative");
            }

            try
            {
                // Check if member with same name already exists
                var existingMember = await _context.Members
                    .FirstOrDefaultAsync(m => m.Name.ToLower() == dto.Name.ToLower());
                
                if (existingMember != null)
                {
                    _logger.LogWarning("User {Username} attempted to create member with duplicate name: {Name}", username, dto.Name);
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

                _logger.LogInformation("User {Username} successfully created member {Id} with name {Name}", username, member.Id, member.Name);

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

                return CreatedAtAction(nameof(GetById), new { id = member.Id }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} failed to create member {Name}", username, dto.Name);
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "MemberManagement")] // Only admins can update members
        public async Task<ActionResult<MemberDto>> Update(int id, [FromBody] CreateUpdateMemberDto dto)
        {
            var username = User.GetUsername();
            _logger.LogInformation("User {Username} attempting to update member {Id}", username, id);

            if (dto == null) 
            {
                _logger.LogWarning("User {Username} attempted to update member {Id} with null request body", username, id);
                return BadRequest("Request body is required");
            }
            
            if (string.IsNullOrWhiteSpace(dto.Name)) 
            {
                _logger.LogWarning("User {Username} attempted to update member {Id} with empty name", username, id);
                return BadRequest("Name is required");
            }
            
            if (dto.CombatPower < 0) 
            {
                _logger.LogWarning("User {Username} attempted to update member {Id} with invalid combat power: {CombatPower}", username, id, dto.CombatPower);
                return BadRequest("Combat power must be non-negative");
            }

            try
            {
                var existing = await _context.Members.FindAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("User {Username} attempted to update non-existent member {Id}", username, id);
                    return NotFound();
                }

                // Check if another member with the same name exists (excluding current member)
                var duplicateMember = await _context.Members
                    .FirstOrDefaultAsync(m => m.Id != id && m.Name.ToLower() == dto.Name.ToLower());
                
                if (duplicateMember != null)
                {
                    _logger.LogWarning("User {Username} attempted to update member {Id} with duplicate name: {Name}", username, id, dto.Name);
                    return BadRequest($"Another member with name '{dto.Name}' already exists");
                }

                existing.Name = dto.Name.Trim();
                existing.CombatPower = dto.CombatPower;
                existing.GcashNumber = dto.GcashNumber?.Trim();
                existing.GcashName = dto.GcashName?.Trim();
                existing.UpdatedAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} successfully updated member {Id}", username, id);

                var responseDto = new MemberDto
                {
                    Id = existing.Id,
                    Name = existing.Name,
                    CombatPower = existing.CombatPower,
                    GcashNumber = existing.GcashNumber,
                    GcashName = existing.GcashName,
                    CreatedAtUtc = existing.CreatedAtUtc,
                    UpdatedAtUtc = existing.UpdatedAtUtc
                };

                return Ok(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} failed to update member {Id}", username, id);
                return StatusCode(500, "Database error occurred");
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "MemberManagement")] // Only admins can delete members
        public async Task<IActionResult> Delete(int id)
        {
            var username = User.GetUsername();
            _logger.LogInformation("User {Username} attempting to delete member {Id}", username, id);

            try
            {
                var existing = await _context.Members.FindAsync(id);
                if (existing == null)
                {
                    _logger.LogWarning("User {Username} attempted to delete non-existent member {Id}", username, id);
                    return NotFound();
                }

                _context.Members.Remove(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Username} successfully deleted member {Id} with name {Name}", username, id, existing.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {Username} failed to delete member {Id}", username, id);
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


}
