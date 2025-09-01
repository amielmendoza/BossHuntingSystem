using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BossHuntingSystem.Server.Models;
using BossHuntingSystem.Server.Services;
using BossHuntingSystem.Server.Extensions;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Allow anonymous access for login
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthenticationService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Request body is required"
                    });
                }

                _logger.LogInformation("Login attempt for user: {Username}", request.Username);

                var response = await _authService.AuthenticateAsync(request);

                if (response.Success)
                {
                    _logger.LogInformation("Successful login for user: {Username}", request.Username);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
                    return Unauthorized(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Username}", request?.Username);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during authentication"
                });
            }
        }

        [HttpPost("validate")]
        [Authorize(Policy = "User")]
        public ActionResult<bool> ValidateToken()
        {
            try
            {
                // If we reach here, the token is valid (JWT middleware validated it)
                var username = User.GetUsername();
                var role = User.GetUserRole();
                
                _logger.LogInformation("Token validation successful for user: {Username} with role: {Role}", username, role);
                
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, false);
            }
        }

        [HttpGet("profile")]
        [Authorize(Policy = "User")]
        public ActionResult<object> GetUserProfile()
        {
            try
            {
                var username = User.GetUsername();
                var role = User.GetUserRole();
                var userId = User.GetUserId();

                var profile = new
                {
                    Username = username,
                    Role = role,
                    UserId = userId,
                    IsAdmin = User.IsAdmin(),
                    IsUser = User.IsUser()
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, "Error retrieving user profile");
            }
        }

        [HttpPost("logout")]
        [Authorize(Policy = "User")]
        public ActionResult Logout()
        {
            try
            {
                var username = User.GetUsername();
                _logger.LogInformation("User logout: {Username}", username);
                
                // Note: JWT tokens are stateless, so we can't invalidate them on the server
                // The client should remove the token from storage
                // For additional security, you could implement a token blacklist
                
                return Ok(new { message = "Logout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, "Error during logout");
            }
        }
    }
}
