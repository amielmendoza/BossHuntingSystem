using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BossHuntingSystem.Server.Data
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, LOGIN, EXPORT, etc.

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty; // Boss, Member, BossDefeat, Client, etc.

        public int? EntityId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? AdditionalInfo { get; set; }

        // Navigation property
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }
    }
}
