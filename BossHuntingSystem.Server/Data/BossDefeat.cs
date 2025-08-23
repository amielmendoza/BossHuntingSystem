using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BossHuntingSystem.Server.Data
{
    public class BossDefeat
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int BossId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string BossName { get; set; } = string.Empty;
        
        // Nullable DateTime - null for history entries, actual time for defeats
        public DateTime? DefeatedAtUtc { get; set; }
        
        // Store loots and attendees as JSON strings
        [Column(TypeName = "nvarchar(max)")]
        public string LootsJson { get; set; } = "[]";
        
        [Column(TypeName = "nvarchar(max)")]
        public string AttendeesJson { get; set; } = "[]";
        
        // Navigation property
        [ForeignKey("BossId")]
        public virtual Boss? Boss { get; set; }
        
        // Not mapped properties for easy access (will be serialized to/from JSON)
        [NotMapped]
        public List<string> Loots
        {
            get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(LootsJson) ?? new List<string>();
            set => LootsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
        
        [NotMapped]
        public List<string> Attendees
        {
            get => System.Text.Json.JsonSerializer.Deserialize<List<string>>(AttendeesJson) ?? new List<string>();
            set => AttendeesJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}
