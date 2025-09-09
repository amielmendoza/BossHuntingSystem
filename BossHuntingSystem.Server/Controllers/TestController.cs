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

        [HttpDelete("cleanup-attendee/{name}")]
        public async Task<ActionResult> CleanupAttendee(string name)
        {
            try
            {
                var defeats = await _context.BossDefeats.ToListAsync();
                var recordsModified = 0;
                
                foreach (var defeat in defeats)
                {
                    var attendeeDetails = defeat.AttendeeDetails;
                    var originalCount = attendeeDetails.Count;
                    
                    // Remove attendees matching the name (case-insensitive)
                    var updatedAttendees = attendeeDetails
                        .Where(a => !string.Equals(a.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    if (updatedAttendees.Count != originalCount)
                    {
                        defeat.AttendeeDetails = updatedAttendees;
                        recordsModified++;
                    }
                }
                
                if (recordsModified > 0)
                {
                    await _context.SaveChangesAsync();
                }
                
                return Ok(new
                {
                    AttendeeNameCleaned = name,
                    RecordsModified = recordsModified,
                    Message = $"Removed '{name}' from {recordsModified} records"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("find-attendee/{name}")]
        public async Task<ActionResult> FindAttendee(string name)
        {
            try
            {
                var defeats = await _context.BossDefeats.ToListAsync();
                var recordsWithAttendee = new List<object>();
                
                foreach (var defeat in defeats)
                {
                    var attendeeDetails = defeat.AttendeeDetails;
                    var foundAttendee = attendeeDetails.FirstOrDefault(a => 
                        string.Equals(a.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
                    
                    if (foundAttendee != null)
                    {
                        recordsWithAttendee.Add(new
                        {
                            RecordId = defeat.Id,
                            BossName = defeat.BossName,
                            DefeatedAt = defeat.DefeatedAtUtc,
                            AttendeeName = foundAttendee.Name,
                            IsLate = foundAttendee.IsLate,
                            Points = foundAttendee.Points
                        });
                    }
                }
                
                return Ok(new
                {
                    SearchName = name,
                    RecordsFound = recordsWithAttendee.Count,
                    Records = recordsWithAttendee
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("analyze-orphaned-data")]
        public async Task<ActionResult> AnalyzeOrphanedData()
        {
            try
            {
                var defeats = await _context.BossDefeats.ToListAsync();
                var history = await _context.BossDefeats.Take(200).OrderByDescending(d => d.DefeatedAtUtc).ToListAsync();
                
                // Get all attendees from points calculation (same logic as GetMemberPoints)
                var allAttendees = new Dictionary<string, (decimal points, int battles, List<int> recordIds)>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var defeat in defeats)
                {
                    var attendeeDetails = defeat.AttendeeDetails;
                    foreach (var attendee in attendeeDetails)
                    {
                        if (!string.IsNullOrWhiteSpace(attendee.Name))
                        {
                            var name = attendee.Name;
                            if (allAttendees.ContainsKey(name))
                            {
                                allAttendees[name] = (
                                    allAttendees[name].points + attendee.Points,
                                    allAttendees[name].battles + 1,
                                    allAttendees[name].recordIds.Concat(new[] { defeat.Id }).ToList()
                                );
                            }
                            else
                            {
                                allAttendees[name] = (attendee.Points, 1, new List<int> { defeat.Id });
                            }
                        }
                    }
                }

                // Get attendees that appear in recent history (last 200 records)
                var recentAttendees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var defeat in history)
                {
                    var attendeeDetails = defeat.AttendeeDetails;
                    foreach (var attendee in attendeeDetails)
                    {
                        if (!string.IsNullOrWhiteSpace(attendee.Name))
                        {
                            recentAttendees.Add(attendee.Name);
                        }
                    }
                }

                // Find potential orphans (appear in points but not in recent history)
                var potentialOrphans = new List<object>();
                var suspiciousEntries = new List<object>();

                foreach (var kvp in allAttendees)
                {
                    var name = kvp.Key;
                    var (points, battles, recordIds) = kvp.Value;
                    
                    // Flag as potential orphan if not in recent history
                    if (!recentAttendees.Contains(name))
                    {
                        potentialOrphans.Add(new
                        {
                            Name = name,
                            Points = points,
                            Battles = battles,
                            RecordIds = recordIds,
                            Reason = "Not found in recent history (last 200 records)"
                        });
                    }
                    
                    // Flag suspicious entries (very low activity, short names, test-like names)
                    if (battles <= 2 && (name.Length <= 3 || 
                        name.All(char.IsLower) && name.All(c => c == name[0]) || // like "qqq", "aaa"
                        name.Equals("test", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("random", StringComparison.OrdinalIgnoreCase) ||
                        name.All(char.IsDigit) ||
                        name.Length == 1 ||
                        name.All(c => "qwertyuiopasdfghjklzxcvbnm".Contains(char.ToLower(c))) && name.Length <= 4))
                    {
                        suspiciousEntries.Add(new
                        {
                            Name = name,
                            Points = points,
                            Battles = battles,
                            RecordIds = recordIds,
                            Reason = battles <= 1 ? "Single attendance" : "Low activity and suspicious name pattern"
                        });
                    }
                }

                return Ok(new
                {
                    TotalAttendees = allAttendees.Count,
                    RecentlyActiveAttendees = recentAttendees.Count,
                    PotentialOrphans = potentialOrphans.Count,
                    SuspiciousEntries = suspiciousEntries.Count,
                    OrphanedData = potentialOrphans,
                    SuspiciousData = suspiciousEntries,
                    RecommendedAction = potentialOrphans.Count > 0 || suspiciousEntries.Count > 0 
                        ? "Review and clean up the flagged entries using cleanup-attendee endpoint"
                        : "No cleanup needed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("cleanup-bulk-attendees")]
        public async Task<ActionResult> CleanupBulkAttendees([FromBody] BulkCleanupRequest request)
        {
            try
            {
                if (request.AttendeeNames == null || !request.AttendeeNames.Any())
                {
                    return BadRequest("AttendeeNames list is required");
                }

                var defeats = await _context.BossDefeats.ToListAsync();
                var totalRecordsModified = 0;
                var cleanupResults = new List<object>();

                foreach (var name in request.AttendeeNames)
                {
                    var recordsModified = 0;
                    
                    foreach (var defeat in defeats)
                    {
                        var attendeeDetails = defeat.AttendeeDetails;
                        var originalCount = attendeeDetails.Count;
                        
                        var updatedAttendees = attendeeDetails
                            .Where(a => !string.Equals(a.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        
                        if (updatedAttendees.Count != originalCount)
                        {
                            defeat.AttendeeDetails = updatedAttendees;
                            recordsModified++;
                        }
                    }
                    
                    totalRecordsModified += recordsModified;
                    cleanupResults.Add(new
                    {
                        Name = name,
                        RecordsModified = recordsModified
                    });
                }

                if (totalRecordsModified > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    TotalAttendeesProcessed = request.AttendeeNames.Count,
                    TotalRecordsModified = totalRecordsModified,
                    Results = cleanupResults,
                    Message = $"Bulk cleanup completed. Removed {request.AttendeeNames.Count} attendees from {totalRecordsModified} total records"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }

    public class BulkCleanupRequest
    {
        public List<string> AttendeeNames { get; set; } = new List<string>();
    }

    public class TestDiscordRequest
    {
        public string BossName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int? MinutesUntilRespawn { get; set; }
        public string? Owner { get; set; }
    }
}
