using BossHuntingSystem.Server.Controllers;

namespace BossHuntingSystem.Server.Services
{
    public class BossNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BossNotificationBackgroundService> _logger;
        private static readonly int[] NotificationMinutes = { 30, 20, 10, 5, 1 };

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
            var bosses = BossesController.GetBossesForNotification();
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
                        _logger.LogInformation("Sending Discord notification for {BossName} - {Minutes} minutes until respawn", 
                            boss.Name, notifyMinutes);

                        await discordService.SendBossNotificationAsync(boss.Name, notifyMinutes);
                        notificationTracker.RecordNotification(boss.Id, notifyMinutes, respawnTime);
                    }
                }
            }
        }
    }
}
