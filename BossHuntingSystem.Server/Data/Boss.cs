using System.ComponentModel.DataAnnotations;

namespace BossHuntingSystem.Server.Data
{
    public class Boss
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public int RespawnHours { get; set; }
        
        [Required]
        public DateTime LastKilledAt { get; set; }
        
        [MaxLength(100)]
        public string? Killer { get; set; }
        
        // Navigation property
        public virtual ICollection<BossDefeat> Defeats { get; set; } = new List<BossDefeat>();
    }
}
