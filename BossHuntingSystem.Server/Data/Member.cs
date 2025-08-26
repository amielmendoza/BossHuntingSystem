using System.ComponentModel.DataAnnotations;

namespace BossHuntingSystem.Server.Data
{
    public class Member
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public int CombatPower { get; set; }
        
        [MaxLength(20)]
        public string? GcashNumber { get; set; }
        
        [MaxLength(100)]
        public string? GcashName { get; set; }
        
        // Timestamp for tracking when the member was first added
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        
        // Timestamp for tracking when the member was last updated
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
