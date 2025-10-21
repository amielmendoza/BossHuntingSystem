using System.ComponentModel.DataAnnotations;

namespace BossHuntingSystem.Server.Data
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LicenseKey { get; set; } = string.Empty;

        [Required]
        public DateTime LicenseExpirationDate { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(200)]
        public string ContactEmail { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        public string? CompanyName { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Grace period in days (default 7 days)
        public int GracePeriodDays { get; set; } = 7;

        // Navigation properties
        public virtual ICollection<Member> Members { get; set; } = new List<Member>();
        public virtual ICollection<Boss> Bosses { get; set; } = new List<Boss>();
        public virtual ICollection<BossDefeat> BossDefeats { get; set; } = new List<BossDefeat>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
