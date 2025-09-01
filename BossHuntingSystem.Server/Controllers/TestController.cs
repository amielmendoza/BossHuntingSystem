using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BossHuntingSystem.Server.Services;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "Write")] // Only admins can use test endpoints
    public class TestController : ControllerBase
    {
        private readonly IDiscordNotificationService _discordService;

        public TestController(IDiscordNotificationService discordService)
        {
            _discordService = discordService;
        }

        [HttpPost("discord")]
        public async Task<IActionResult> TestDiscordNotification([FromBody] TestDiscordRequest request)
        {
            if (string.IsNullOrEmpty(request.BossName))
            {
                return BadRequest("Boss name is required");
            }

            try
            {
                await _discordService.SendBossNotificationAsync(
                    request.BossName, 
                    request.MinutesUntilRespawn ?? 5);

                return Ok(new { message = "Discord notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class TestDiscordRequest
    {
        public string BossName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int? MinutesUntilRespawn { get; set; }
    }
}
