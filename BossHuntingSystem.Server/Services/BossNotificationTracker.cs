using BossHuntingSystem.Server.Models;

namespace BossHuntingSystem.Server.Services
{
    public interface IBossNotificationTracker
    {
        bool ShouldSendNotification(int bossId, int minutesBeforeRespawn, DateTime bossRespawnTime);
        void RecordNotification(int bossId, int minutesBeforeRespawn, DateTime bossRespawnTime);
        void CleanupOldNotifications();
    }

    public class BossNotificationTracker : IBossNotificationTracker
    {
        private static readonly List<BossNotification> SentNotifications = new();
        private static readonly object Lock = new();
        private readonly ILogger<BossNotificationTracker> _logger;

        public BossNotificationTracker(ILogger<BossNotificationTracker> logger)
        {
            _logger = logger;
        }

        public bool ShouldSendNotification(int bossId, int minutesBeforeRespawn, DateTime bossRespawnTime)
        {
            lock (Lock)
            {
                // Check if we've already sent this specific notification for this respawn cycle
                var existingNotification = SentNotifications.FirstOrDefault(n => 
                    n.BossId == bossId && 
                    n.MinutesBeforeRespawn == minutesBeforeRespawn &&
                    Math.Abs((n.BossRespawnTime - bossRespawnTime).TotalMinutes) < 1); // Within 1 minute tolerance

                var shouldSend = existingNotification == null;
                
                _logger.LogDebug("Notification check for Boss {BossId}, {Minutes}min before respawn at {RespawnTime}: {ShouldSend}", 
                    bossId, minutesBeforeRespawn, bossRespawnTime, shouldSend ? "SEND" : "SKIP");
                
                if (existingNotification != null)
                {
                    _logger.LogDebug("Existing notification found: sent at {SentAt} for respawn at {ExistingRespawnTime}", 
                        existingNotification.SentAt, existingNotification.BossRespawnTime);
                }

                return shouldSend;
            }
        }

        public void RecordNotification(int bossId, int minutesBeforeRespawn, DateTime bossRespawnTime)
        {
            lock (Lock)
            {
                var notification = new BossNotification
                {
                    BossId = bossId,
                    MinutesBeforeRespawn = minutesBeforeRespawn,
                    SentAt = DateTime.UtcNow,
                    BossRespawnTime = bossRespawnTime
                };

                SentNotifications.Add(notification);
                
                _logger.LogDebug("Recorded notification for Boss {BossId}, {Minutes}min before respawn at {RespawnTime}, sent at {SentAt}", 
                    bossId, minutesBeforeRespawn, bossRespawnTime, notification.SentAt);
            }
        }

        public void CleanupOldNotifications()
        {
            lock (Lock)
            {
                // Remove notifications older than 2 hours
                var cutoff = DateTime.UtcNow.AddHours(-2);
                SentNotifications.RemoveAll(n => n.SentAt < cutoff);
            }
        }
    }
}
