namespace BossHuntingSystem.Server.Models
{
    public class BossNotification
    {
        public int BossId { get; set; }
        public int MinutesBeforeRespawn { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime BossRespawnTime { get; set; }
    }
}
