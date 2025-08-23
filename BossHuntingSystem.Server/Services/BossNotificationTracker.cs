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

        public bool ShouldSendNotification(int bossId, int minutesBeforeRespawn, DateTime bossRespawnTime)
        {
            lock (Lock)
            {
                // Check if we've already sent this specific notification for this respawn cycle
                return !SentNotifications.Any(n => 
                    n.BossId == bossId && 
                    n.MinutesBeforeRespawn == minutesBeforeRespawn &&
                    Math.Abs((n.BossRespawnTime - bossRespawnTime).TotalMinutes) < 1); // Within 1 minute tolerance
            }
        }

        public void RecordNotification(int bossId, int minutesBeforeRespawn, DateTime bossRespawnTime)
        {
            lock (Lock)
            {
                SentNotifications.Add(new BossNotification
                {
                    BossId = bossId,
                    MinutesBeforeRespawn = minutesBeforeRespawn,
                    SentAt = DateTime.UtcNow,
                    BossRespawnTime = bossRespawnTime
                });
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
