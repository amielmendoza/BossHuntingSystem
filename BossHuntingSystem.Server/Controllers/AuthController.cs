using Microsoft.AspNetCore.Mvc;
using BossHuntingSystem.Server.Models;
using BossHuntingSystem.Server.Services;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        public ActionResult<bool> ValidateToken([FromBody] string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(false);
                }

                var isValid = _authService.ValidateToken(token);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, false);
            }
        }
    }
}
