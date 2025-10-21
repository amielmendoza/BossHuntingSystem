using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Services;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly BossHuntingDbContext _context;
        private readonly IAuditLogService _auditLogService;

        public UsersController(BossHuntingDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        private int GetCurrentClientId()
        {
            var clientId = HttpContext.Items["ClientId"] as int?;
            if (!clientId.HasValue)
                throw new UnauthorizedAccessException("Client context not found");
            return clientId.Value;
        }

        private string GetCurrentUsername()
        {
            return User.Identity?.Name ?? "Unknown";
        }

        private bool IsSuperAdmin()
        {
            return User.IsInRole("SuperAdmin");
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        // GET: api/users
        [HttpGet]
        [Authorize(Roles = "Manager,Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var clientId = GetCurrentClientId();

            var users = await _context.Users
                .Where(u => u.ClientId == clientId)
                .OrderBy(u => u.Username)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    ClientId = u.ClientId,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Manager,Admin,SuperAdmin")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var clientId = GetCurrentClientId();

            var user = await _context.Users
                .Where(u => u.Id == id && u.ClientId == clientId)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    ClientId = u.ClientId,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        // POST: api/users
        [HttpPost]
        [Authorize(Roles = "Manager,Admin,SuperAdmin")]
        public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
        {
            var clientId = GetCurrentClientId();

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { message = "Username is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Password is required" });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters" });
            }

            // Check if username already exists in this client
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.ClientId == clientId);

            if (existingUser != null)
            {
                return BadRequest(new { message = $"Username '{request.Username}' already exists" });
            }

            // Validate role
            var validRoles = new[] { "User", "Manager", "Admin" };
            if (!validRoles.Contains(request.Role))
            {
                return BadRequest(new { message = "Invalid role. Must be 'User', 'Manager', or 'Admin'" });
            }

            // Create user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
                Role = request.Role,
                ClientId = clientId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditLogService.LogActionAsync(
                clientId,
                GetCurrentUsername(),
                "CREATE",
                "User",
                user.Id,
                oldValues: null,
                newValues: new { user.Username, user.Email, user.Role, user.IsActive },
                ipAddress: GetClientIpAddress()
            );

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                ClientId = user.ClientId,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }

        // PUT: api/users/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Admin,SuperAdmin")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest request)
        {
            var clientId = GetCurrentClientId();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.ClientId == clientId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Store old values for audit
            var oldValues = new
            {
                user.Email,
                user.Role,
                user.IsActive
            };

            // Validate email
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            // Validate role
            var validRoles = new[] { "User", "Manager", "Admin" };
            if (!validRoles.Contains(request.Role))
            {
                return BadRequest(new { message = "Invalid role. Must be 'User', 'Manager', or 'Admin'" });
            }

            // Update user
            user.Email = request.Email;
            user.Role = request.Role;
            user.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            // Log the action
            await _auditLogService.LogActionAsync(
                clientId,
                GetCurrentUsername(),
                "UPDATE",
                "User",
                user.Id,
                oldValues: oldValues,
                newValues: new { user.Email, user.Role, user.IsActive },
                ipAddress: GetClientIpAddress()
            );

            return NoContent();
        }

        // POST: api/users/5/change-password
        [HttpPost("{id}/change-password")]
        [Authorize(Roles = "Manager,Admin,SuperAdmin")]
        public async Task<IActionResult> ChangePassword(int id, AdminChangePasswordRequest request)
        {
            var clientId = GetCurrentClientId();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.ClientId == clientId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { message = "New password is required" });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "Password must be at least 6 characters" });
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);

            await _context.SaveChangesAsync();

            // Log the action
            await _auditLogService.LogActionAsync(
                clientId,
                GetCurrentUsername(),
                "PASSWORD_CHANGE",
                "User",
                user.Id,
                oldValues: null,
                newValues: new { Message = "Password changed" },
                ipAddress: GetClientIpAddress()
            );

            return Ok(new { message = "Password changed successfully" });
        }

        // DELETE: api/users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var clientId = GetCurrentClientId();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.ClientId == clientId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Store old values for audit
            var oldValues = new
            {
                user.Username,
                user.Email,
                user.Role,
                user.IsActive
            };

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Log the action
            await _auditLogService.LogActionAsync(
                clientId,
                GetCurrentUsername(),
                "DELETE",
                "User",
                id,
                oldValues: oldValues,
                newValues: null,
                ipAddress: GetClientIpAddress()
            );

            return NoContent();
        }
    }

    // DTOs
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AdminChangePasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
