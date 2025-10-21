using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BossHuntingSystem.Server.Data;
using BossHuntingSystem.Server.Models;
using BossHuntingSystem.Server.Services;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly BossHuntingDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            BossHuntingDbContext context,
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Client)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning($"Failed login attempt for username: {request.Username}");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            if (!user.IsActive)
            {
                _logger.LogWarning($"Login attempt for inactive user: {request.Username}");
                return Unauthorized(new { message = "User account is inactive" });
            }

            if (user.Client == null || !user.Client.IsActive)
            {
                _logger.LogWarning($"Login attempt for user with inactive client: {request.Username}");
                return Unauthorized(new { message = "Client account is inactive" });
            }

            // Update last login date
            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);

            // Log the login
            await _auditLogService.LogActionAsync(
                user.ClientId,
                user.Username,
                "LOGIN",
                "User",
                user.Id,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "User logged in successfully"
            );

            return Ok(new LoginResponse
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                ClientId = user.ClientId,
                ClientName = user.Client.Name
            });
        }

        [HttpPost("register")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<ActionResult<User>> Register(RegisterRequest request)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new { message = "Username already exists" });
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Get ClientId from context for non-SuperAdmin users
            var currentUserRole = HttpContext.Items["UserRole"] as string;
            int clientId;

            if (currentUserRole == "SuperAdmin")
            {
                // SuperAdmin can specify ClientId
                if (!request.ClientId.HasValue)
                {
                    return BadRequest(new { message = "ClientId is required for SuperAdmin to create users" });
                }
                clientId = request.ClientId.Value;
            }
            else
            {
                // Admin can only create users for their own client
                var currentClientId = HttpContext.Items["ClientId"] as int?;
                if (!currentClientId.HasValue)
                {
                    return BadRequest(new { message = "Client context not found" });
                }
                clientId = currentClientId.Value;
            }

            // Verify client exists
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null)
            {
                return BadRequest(new { message = "Client not found" });
            }

            // Only SuperAdmin can create SuperAdmin or Admin users
            // Admin can create Manager users
            if (request.Role == "SuperAdmin" && currentUserRole != "SuperAdmin")
            {
                return Forbid();
            }

            if (request.Role == "Admin" && currentUserRole != "SuperAdmin")
            {
                return Forbid();
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                Role = request.Role ?? "User",
                ClientId = clientId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var currentUsername = HttpContext.Items["Username"] as string ?? "System";
            await _auditLogService.LogActionAsync(
                clientId,
                currentUsername,
                "CREATE",
                "User",
                user.Id,
                null,
                new { user.Username, user.Email, user.Role },
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                $"New user created: {user.Username}"
            );

            // Remove password hash from response
            user.PasswordHash = string.Empty;

            return CreatedAtAction(nameof(Login), new { username = user.Username }, user);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var username = HttpContext.Items["Username"] as string;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Current password is incorrect" });
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                user.ClientId,
                user.Username,
                "CHANGE_PASSWORD",
                "User",
                user.Id,
                null,
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "Password changed successfully"
            );

            return Ok(new { message = "Password changed successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfoResponse>> GetCurrentUser()
        {
            var username = HttpContext.Items["Username"] as string;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .Include(u => u.Client)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(new UserInfoResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                ClientId = user.ClientId,
                ClientName = user.Client?.Name ?? "",
                IsActive = user.IsActive,
                LastLoginDate = user.LastLoginDate
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JWT");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"] ?? "BossHuntingSystem";
            var audience = jwtSettings["Audience"] ?? "BossHuntingSystemUsers";
            var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "480");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("ClientId", user.ClientId.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            // Use BCrypt with work factor of 12 for security
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        private bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                // Handle cases where hash might be in old SHA256 format or corrupted
                return false;
            }
        }

        // TEMPORARY: Reset default passwords - REMOVE AFTER FIRST USE
        [HttpPost("reset-defaults")]
        public async Task<IActionResult> ResetDefaultPasswords()
        {
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            var superadminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "superadmin");

            if (adminUser != null)
            {
                adminUser.PasswordHash = HashPassword("Admin@123");
            }

            if (superadminUser != null)
            {
                superadminUser.PasswordHash = HashPassword("SuperAdmin@123");
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Default passwords reset successfully",
                adminHash = adminUser?.PasswordHash,
                superadminHash = superadminUser?.PasswordHash
            });
        }
    }
}
