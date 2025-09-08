using BossHuntingSystem.Server.Controllers;
using BossHuntingSystem.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace BossHuntingSystem.Server.Services
{
    public class BossNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BossNotificationBackgroundService> _logger;
        private static readonly int[] NotificationMinutes = { 30, 20, 10, 5, 1 };
        private static readonly int[] PointsSummaryHours = { 0, 6, 12, 18 }; // 12 AM, 6 AM, 12 PM, 6 PM
        private DateTime _lastPointsSummaryNotification = DateTime.MinValue;

        public BossNotificationBackgroundService(IServiceProvider serviceProvider, ILogger<BossNotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Boss notification service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var discordService = scope.ServiceProvider.GetRequiredService<IDiscordNotificationService>();
                    var notificationTracker = scope.ServiceProvider.GetRequiredService<IBossNotificationTracker>();

                    await CheckBossNotifications(discordService, notificationTracker);
                    
                    // Check for points summary every 6 hours (12 AM, 6 AM, 12 PM, 6 PM PHT)
                    await CheckPointsSummaryNotifications(discordService);
                    
                    // Cleanup old notifications every hour
                    if (DateTime.UtcNow.Minute == 0)
                    {
                        notificationTracker.CleanupOldNotifications();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in boss notification service");
                }

                // Check every 30 seconds for more responsive notifications
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task CheckBossNotifications(IDiscordNotificationService discordService, IBossNotificationTracker notificationTracker)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BossHuntingDbContext>();
            
            var bosses = await context.Bosses.ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var boss in bosses)
            {
                var respawnTime = boss.LastKilledAt.AddHours(boss.RespawnHours);
                var timeUntilRespawn = respawnTime - now;

                foreach (var notifyMinutes in NotificationMinutes)
                {
                    var notificationTime = respawnTime.AddMinutes(-notifyMinutes);
                    var timeDifference = Math.Abs((now - notificationTime).TotalMinutes);

                    // Send notification if we're within 30 seconds of the notification time
                    if (timeDifference <= 0.5 && notificationTracker.ShouldSendNotification(boss.Id, notifyMinutes, respawnTime))
                    {
                        _logger.LogInformation("Sending Discord notification for {BossName} - {Minutes} minutes until respawn, owned by {Owner}", 
                            boss.Name, notifyMinutes, boss.Owner ?? "Unknown");

                        await discordService.SendBossNotificationAsync(boss.Name, notifyMinutes, boss.Owner);
                        notificationTracker.RecordNotification(boss.Id, notifyMinutes, respawnTime);
                    }
                }
            }
        }

        private async Task CheckPointsSummaryNotifications(IDiscordNotificationService discordService)
        {
            try
            {
                // Convert UTC to Philippine Time (PHT = UTC+8)
                var phtNow = DateTime.UtcNow.AddHours(8);
                var currentTime = new DateTime(phtNow.Year, phtNow.Month, phtNow.Day, phtNow.Hour, 0, 0);

                // Check if current hour is one of our notification hours (12 AM, 6 AM, 12 PM, 6 PM)
                // and we're at the top of the hour (minute 0) and haven't sent for this exact time
                if (PointsSummaryHours.Contains(phtNow.Hour) && 
                    phtNow.Minute == 0 && 
                    _lastPointsSummaryNotification != currentTime)
                {
                    var timeDescription = phtNow.Hour switch
                    {
                        0 => "12:00 AM (Midnight)",
                        6 => "6:00 AM (Morning)",
                        12 => "12:00 PM (Noon)",
                        18 => "6:00 PM (Evening)",
                        _ => $"{phtNow.Hour}:00"
                    };

                    _logger.LogInformation("Sending points summary at {TimeDescription} PHT", timeDescription);

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<BossHuntingDbContext>();

                    // Get member points
                    var memberPoints = await GetMemberPointsFromDatabase(context);

                    if (memberPoints.Any())
                    {
                        await discordService.SendDailyPointsSummaryAsync(memberPoints);
                        _lastPointsSummaryNotification = currentTime;
                        _logger.LogInformation("Points summary sent successfully for {MemberCount} members at {TimeDescription}", 
                            memberPoints.Count, timeDescription);
                    }
                    else
                    {
                        _logger.LogInformation("No member points data found for points summary at {TimeDescription}", timeDescription);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking points summary notifications");
            }
        }

        private async Task<List<MemberPointsDto>> GetMemberPointsFromDatabase(BossHuntingDbContext context)
        {
            try
            {
                // Get all boss defeats with attendee details
                var defeats = await context.BossDefeats.ToListAsync();
                
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
                _logger.LogError(ex, "Error calculating member points for daily summary");
                return new List<MemberPointsDto>();
            }
        }
    }
}
