using Microsoft.AspNetCore.Mvc;
using BossHuntingSystem.Server.Services;
using BossHuntingSystem.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace BossHuntingSystem.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IDiscordNotificationService _discordService;
        private readonly BossHuntingDbContext _context;

        public TestController(IDiscordNotificationService discordService, BossHuntingDbContext context)
        {
            _discordService = discordService;
            _context = context;
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
                    request.MinutesUntilRespawn ?? 5, 
                    request.Owner);

                return Ok(new { message = "Discord notification sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("daily-points-summary")]
        public async Task<IActionResult> TestDailyPointsSummary()
        {
            try
            {
                // Get member points using same logic as the background service
                var memberPoints = await GetMemberPointsFromDatabase();

                if (!memberPoints.Any())
                {
                    return Ok(new { message = "No member points data found" });
                }

                await _discordService.SendDailyPointsSummaryAsync(memberPoints);

                return Ok(new { 
                    message = "Daily points summary sent successfully",
                    memberCount = memberPoints.Count,
                    totalPoints = memberPoints.Sum(m => m.Points)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<List<MemberPointsDto>> GetMemberPointsFromDatabase()
        {
            try
            {
                // Get all boss defeats with attendee details
                var defeats = await _context.BossDefeats.ToListAsync();
                
                // Calculate points per member
                // Using case-insensitive comparison to handle different casing of member names
                var memberPointsDict = new Dictionary<string, (decimal points, int bossesAttended)>(StringComparer.OrdinalIgnoreCase);

                foreach (var defeat in defeats)
                {
                    var attendeeDetails = defeat.AttendeeDetails;
                    
                    foreach (var attendee in attendeeDetails)
                    {
                        var memberName = attendee.Name;
                        if (memberPointsDict.ContainsKey(memberName))
                        {
                            memberPointsDict[memberName] = (
                                memberPointsDict[memberName].points + attendee.Points,
                                memberPointsDict[memberName].bossesAttended + 1
                            );
                        }
                        else
                        {
                            memberPointsDict[memberName] = (attendee.Points, 1);
                        }
                    }
                }

                // Convert to DTO list
                return memberPointsDict
                    .Select(kvp => new MemberPointsDto
                    {
                        MemberName = kvp.Key,
                        Points = kvp.Value.points,
                        BossesAttended = kvp.Value.bossesAttended
                    })
                    .OrderByDescending(m => m.Points)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating member points: {ex.Message}");
            }
        }
    }

    public class TestDiscordRequest
    {
        public string BossName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int? MinutesUntilRespawn { get; set; }
        public string? Owner { get; set; }
    }
}
