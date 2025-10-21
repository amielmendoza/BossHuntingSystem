using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BossHuntingSystem.Server.Data
{
    public class Boss
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int RespawnHours { get; set; }

        [Required]
        public DateTime LastKilledAt { get; set; }

        [MaxLength(100)]
        public string? Owner { get; set; }

        // Navigation properties
        public virtual ICollection<BossDefeat> Defeats { get; set; } = new List<BossDefeat>();

        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }
    }
}
